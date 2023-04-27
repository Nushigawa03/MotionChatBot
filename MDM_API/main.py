import MDM
motion_length = 6.0
model, diffusion, data, n_frames = MDM.Load(motion_length = motion_length)

from fastapi import FastAPI
app = FastAPI()

@app.get("/")
def read_root():
    return {"Hello": "World"}

@app.get("/MDM/")
async def create_item(text_prompt: str = "The person suddenly dances while walking."):
    rvalue = {}

    text_prompt = "The person suddenly dances while walking."
    model_kwargs = MDM.Get_kwargs(text_prompt, n_frames)

    motion = MDM.Generate(model, diffusion, data, model_kwargs, n_frames)
    rvalue["motion"] = motion.tolist()
    return rvalue

@app.get("/items/{item_id}")
def read_item(item_id: int, q: str = None):
    return {"item_id": item_id, "q": q}