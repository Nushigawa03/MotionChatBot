using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MotionChat
{
    public class Play : MonoBehaviour
    {
        GameObject[] points = new GameObject[22];
        int index = -1;

        public GameObject model;
        Animator animator;
        float[,,,] difMotion;
        int difMotion_frame;
        APIClient _APIClient;

        // Start is called before the first frame update
        async void Start()
        {
            _APIClient = new APIClient();
            // ポイント群の生成
            for (int i = 0; i < 22; i ++)
            {
                points[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                points[i].name = "Sphere_"+i;
                points[i].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }

            // アニメータの取得
            this.animator = this.model.GetComponent<Animator>();

            string user_message = "こんにちは、今日もいい天気ですね。野球のバットの素振りをしてくれませんか？";
            Debug.Log("User:" + user_message);
            var chatMotionReaction = await _APIClient.Chat(user_message);
            Debug.Log("ChatGPT:" + chatMotionReaction.message);
            var text_prompt = chatMotionReaction.motion;
            this.difMotion = await APIClient.GetMotion(text_prompt); // [1,22,3,frame]
            this.difMotion_frame = this.difMotion.GetLength(3);
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (this.difMotion is null) return;
            // ポイント群の更新
            index = (index + 1) % this.difMotion_frame;
            if (index >= this.difMotion_frame) return;    // this.difMotion.GetLength(3)
            for (int i = 0; i < 22; i++) // this.difMotion.GetLength(1)
            {
                Vector3 pos = this.points[i].transform.position;
                pos.x = this.difMotion[0,i,0,this.index]+1;
                pos.y = this.difMotion[0,i,1,this.index];
                pos.z = this.difMotion[0,i,2,this.index];
                this.points[i].transform.position = pos;
            }

            //HipsのTransformの取得
            Transform hipsTransform = this.animator.GetBoneTransform(HumanBodyBones.Hips);
            
            // Hipsの位置の更新
            Vector3 p = hipsTransform.position;
            p.x = this.difMotion[0,0,0,this.index];
            p.y = this.difMotion[0,0,1,this.index];
            p.z = this.difMotion[0,0,2,this.index];
            hipsTransform.position = p;

            Vector3 v1 = new Vector3();
            v1.x = this.difMotion[0,0,0,this.index];
            v1.y = this.difMotion[0,0,1,this.index]*0;
            v1.z = this.difMotion[0,0,2,this.index];

            Vector3 v2 = new Vector3();
            v2.x = this.difMotion[0,3,0,this.index];
            v2.y = this.difMotion[0,3,1,this.index]*0;
            v2.z = this.difMotion[0,3,2,this.index];
            
            // hipsTransform.rotation = Quaternion.FromToRotation(new Vector3(0,1,0), v1-v2);
            hipsTransform.rotation = Quaternion.FromToRotation(Vector3.forward, v2-v1);
            hipsTransform.rotation *= Quaternion.AngleAxis(-90, Vector3.forward);
            hipsTransform.rotation *= Quaternion.AngleAxis(-90, Vector3.right);

            // 左足の更新
            UpdateBoneRotate(HumanBodyBones.LeftUpperLeg, 2, 5, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.LeftLowerLeg, 5, 8, -hipsTransform.right, -90, -90, 0);
            UpdateBoneRotate(HumanBodyBones.LeftFoot, 8, 11, -hipsTransform.right, -90, -90, 0);

            // 右足の更新
            UpdateBoneRotate(HumanBodyBones.RightUpperLeg, 1, 4, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.RightLowerLeg, 4, 7, -hipsTransform.right, -90, -90, 0);
            UpdateBoneRotate(HumanBodyBones.RightFoot, 7, 10, -hipsTransform.right, -90, -90, 0);

            // 左手の更新
            UpdateBoneRotate(HumanBodyBones.LeftShoulder, 14, 17, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.LeftUpperArm, 17, 19, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.LeftLowerArm, 19, 21, -hipsTransform.right, -90, -90, 0);

            // 右手の更新
            UpdateBoneRotate(HumanBodyBones.RightShoulder, 13, 16, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.RightUpperArm, 16, 18, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.RightLowerArm, 18, 20, -hipsTransform.right, -90, -90, 0);

            // 背骨の更新
            UpdateBoneRotate(HumanBodyBones.Head, 12, 15, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.Neck, 9, 15, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.Chest, 6, 9, -hipsTransform.right, -90, 90, 0);
            UpdateBoneRotate(HumanBodyBones.Spine, 3, 6,  -hipsTransform.right, -90, 90, 0);
        }

        // ボーン向きの更新
        void UpdateBoneRotate(HumanBodyBones bone, int p1, int p2, Vector3 axis, int s1, int s2, int s3)
        {
            Vector3 v1 = new Vector3();
            v1.x = this.difMotion[0,p1,0,this.index];
            v1.y = this.difMotion[0,p1,1,this.index];
            v1.z = this.difMotion[0,p1,2,this.index];

            Vector3 v2 = new Vector3();
            v2.x = this.difMotion[0,p2,0,this.index];
            v2.y = this.difMotion[0,p2,1,this.index];
            v2.z = this.difMotion[0,p2,2,this.index];

            this.animator.GetBoneTransform(bone).rotation = Quaternion.FromToRotation(axis, v2-v1);
            this.animator.GetBoneTransform(bone).rotation *= Quaternion.AngleAxis(s1, Vector3.forward);
            this.animator.GetBoneTransform(bone).rotation *= Quaternion.AngleAxis(s2, Vector3.right);
            this.animator.GetBoneTransform(bone).rotation *= Quaternion.AngleAxis(s3, Vector3.up);
        }
    }
}
