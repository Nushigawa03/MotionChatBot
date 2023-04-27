import sys
sys.path.append('motion-diffusion-model')

from utils.fixseed import fixseed
import os
import numpy as np
import torch
from utils.parser_util import generate_args
from utils.model_util import load_model_wo_clip
from utils import dist_util
from model.cfg_sampler import ClassifierFreeSampleModel
from data_loaders.get_data import get_dataset_loader
from data_loaders.humanml.scripts.motion_process import recover_from_ric
import data_loaders.humanml.utils.paramUtil as paramUtil
from data_loaders.humanml.utils.plot_script import plot_3d_motion
import shutil
from data_loaders.tensors import collate

from model.mdm import MDM
from diffusion import gaussian_diffusion as gd
from diffusion.respace import SpacedDiffusion, space_timesteps

os.chdir('motion-diffusion-model')


def load_dataset(dataset, batch_size, max_frames, n_frames):
    data = get_dataset_loader(name=dataset,
                              batch_size=batch_size,
                              num_frames=max_frames,
                              split='test',
                              hml_mode='text_only')
    data.fixed_length = n_frames
    return data

def create_model_and_diffusion(dataset, data):
    model = MDM(**get_model_args(dataset, data))
    diffusion = create_gaussian_diffusion()
    return model, diffusion

def get_model_args(dataset, data):

    # default args
    clip_version = 'ViT-B/32'
    action_emb = 'tensor'
    cond_mode = 'text'
    if hasattr(data.dataset, 'num_actions'):
        num_actions = data.dataset.num_actions
    else:
        num_actions = 1

    # SMPL defaults
    data_rep = 'rot6d'
    njoints = 25
    nfeats = 6

    if dataset == 'humanml':
        data_rep = 'hml_vec'
        njoints = 263
        nfeats = 1
    elif dataset == 'kit':
        data_rep = 'hml_vec'
        njoints = 251
        nfeats = 1
    
    # args
    layers = 8
    latent_dim = 512
    cond_mask_prob = .1
    arch = 'trans_enc'
    emb_trans_dec = False

    print(cond_mode)

    return {'modeltype': '', 'njoints': njoints, 'nfeats': nfeats, 'num_actions': num_actions,
            'translation': True, 'pose_rep': 'rot6d', 'glob': True, 'glob_rot': True,
            'latent_dim': latent_dim, 'ff_size': 1024, 'num_layers': layers, 'num_heads': 4,
            'dropout': 0.1, 'activation': "gelu", 'data_rep': data_rep, 'cond_mode': cond_mode,
            'cond_mask_prob': cond_mask_prob, 'action_emb': action_emb, 'arch': arch,
            'emb_trans_dec': emb_trans_dec, 'clip_version': clip_version, 'dataset': dataset}

def create_gaussian_diffusion():
    # default params
    predict_xstart = True  # we always predict x_start (a.k.a. x0), that's our deal!
    steps = 1000
    scale_beta = 1.  # no scaling
    timestep_respacing = ''  # can be used for ddim sampling, we don't use it.
    learn_sigma = False
    rescale_timesteps = False

    noise_schedule = 'cosine'
    betas = gd.get_named_beta_schedule(noise_schedule, steps, scale_beta)
    loss_type = gd.LossType.MSE

    if not timestep_respacing:
        timestep_respacing = [steps]

    # args
    sigma_small=True
    lambda_vel=0.0
    lambda_rcxyz=0.0
    lambda_fc=0.0

    return SpacedDiffusion(
        use_timesteps=space_timesteps(steps, timestep_respacing),
        betas=betas,
        model_mean_type=(
            gd.ModelMeanType.EPSILON if not predict_xstart else gd.ModelMeanType.START_X
        ),
        model_var_type=(
            (
                gd.ModelVarType.FIXED_LARGE
                if not sigma_small
                else gd.ModelVarType.FIXED_SMALL
            )
            if not learn_sigma
            else gd.ModelVarType.LEARNED_RANGE
        ),
        loss_type=loss_type,
        rescale_timesteps=rescale_timesteps,
        lambda_vel=lambda_vel,
        lambda_rcxyz=lambda_rcxyz,
        lambda_fc=lambda_fc,
    )



def Load(motion_length = 6.0, batch_size=1, guidance_param = 2.5):
    model_path = "./save/humanml_trans_enc_512/model000200000.pt"
    dataset = 'humanml'
    seed = None

    name = os.path.basename(os.path.dirname(model_path))
    niter = os.path.basename(model_path).replace('model', '').replace('.pt', '')
    max_frames = 196 if dataset in ['kit', 'humanml'] else 60
    fps = 12.5 if dataset == 'kit' else 20
    n_frames = min(max_frames, int(motion_length*fps))

    if seed is not None: fixseed(seed)
    dist_util.setup_dist(0)

    print('Loading dataset...')
    data = load_dataset(dataset, batch_size=batch_size, max_frames=max_frames, n_frames=n_frames)

    print("Creating model and diffusion...")
    model, diffusion = create_model_and_diffusion(dataset, data)

    print(f"Loading checkpoints from [{model_path}]...")
    state_dict = torch.load(model_path, map_location='cpu')
    load_model_wo_clip(model, state_dict)

    if guidance_param != 1:
        model = ClassifierFreeSampleModel(model)   # wrapping model with the classifier-free sampler
    model.to(dist_util.dev())
    model.eval()  # disable random masking

    return model, diffusion, data, n_frames

def Get_kwargs(text_prompt, n_frames):
    texts = [text_prompt]
    collate_args = [{'inp': torch.zeros(n_frames), 'tokens': None, 'lengths': n_frames}]
    collate_args = [dict(arg, text=txt) for arg, txt in zip(collate_args, texts)]
    _, model_kwargs = collate(collate_args)
    return model_kwargs

def Generate(model, diffusion, data, model_kwargs, n_frames, batch_size=1, guidance_param = 2.5):
    print(f'### Sampling')

    # add CFG scale to batch
    if guidance_param != 1:
        model_kwargs['y']['scale'] = torch.ones(batch_size, device=dist_util.dev()) * guidance_param

    sample_fn = diffusion.p_sample_loop

    sample = sample_fn(
        model,
        (batch_size, model.njoints, model.nfeats, n_frames),
        clip_denoised=False,
        model_kwargs=model_kwargs,
        skip_timesteps=0,  # 0 is the default value - i.e. don't skip any step
        init_image=None,
        progress=True,
        dump_steps=None,
        noise=None,
        const_noise=False,
    )
    s = sample.clone()

    # Recover XYZ *positions* from HumanML3D vector representation
    if model.data_rep == 'hml_vec':
        n_joints = 22 if sample.shape[1] == 263 else 21
        sample = data.dataset.t2m_dataset.inv_transform(sample.cpu().permute(0, 2, 3, 1)).float()
        sample = recover_from_ric(sample, n_joints)
        sample = sample.view(-1, *sample.shape[2:]).permute(0, 2, 3, 1)

    rot2xyz_pose_rep = 'xyz' if model.data_rep in ['xyz', 'hml_vec'] else model.data_rep
    rot2xyz_mask = None if rot2xyz_pose_rep == 'xyz' else model_kwargs['y']['mask'].reshape(batch_size, n_frames).bool()
    sample = model.rot2xyz(x=sample, mask=rot2xyz_mask, pose_rep=rot2xyz_pose_rep, glob=True, translation=True,
                            jointstype='smpl', vertstrans=True, betas=None, beta=0, glob_rot=None,
                            get_rotations_back=False)

    print("created sample")
    
    return sample.cpu().numpy()


def main():
    model, diffusion, data, n_frames = Load()

    text_prompt = "The person suddenly dances while walking."
    model_kwargs = Get_kwargs(text_prompt, n_frames)

    motion = Generate(model, diffusion, data, model_kwargs, n_frames)
    print(type(motion))

if __name__ == "__main__":
    main()