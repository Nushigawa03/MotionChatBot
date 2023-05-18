using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace MotionChat
{
    public class Play : MonoBehaviour
    {
        public GameObject model;
        Animator animator;
        public float[,,,] difMotion;

        GameObject[] points = new GameObject[22];
        int index = -1;

        async void Start()
        {
            // ポイント群の生成
            for (int i = 0; i < 22; i ++)
            {
                points[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                points[i].name = "Sphere_"+i;
                points[i].transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            }

            // 取得
            this.animator = this.model.GetComponent<Animator>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (this.difMotion is null) return;
            int difMotion_frame = this.difMotion.GetLength(3);
            // ポイント群の更新
            index = (index + 1) % difMotion_frame;
            if (index >= difMotion_frame) return;    // this.difMotion.GetLength(3)
            for (int i = 0; i < 22; i++) // this.difMotion.GetLength(1)
            {
                Vector3 pos = this.points[i].transform.position;
                pos.x = this.difMotion[0,i,0,this.index];
                pos.y = this.difMotion[0,i,1,this.index];
                pos.z = this.difMotion[0,i,2,this.index];
                this.points[i].transform.position = pos;
            }

            //HipsのTransformの取得
            Transform hipsTransform = this.animator.GetBoneTransform(HumanBodyBones.Hips);

            // Hipsの位置の更新
            Vector3 p0;
            p0.x = this.difMotion[0,0,0,this.index];
            p0.y = this.difMotion[0,0,1,this.index];
            p0.z = this.difMotion[0,0,2,this.index];
            hipsTransform.position = p0;
            
            Vector3 p1;
            p1.x = this.difMotion[0,1,0,this.index];
            p1.y = this.difMotion[0,1,1,this.index];
            p1.z = this.difMotion[0,1,2,this.index];
            Vector3 p2;
            p2.x = this.difMotion[0,2,0,this.index];
            p2.y = this.difMotion[0,2,1,this.index];
            p2.z = this.difMotion[0,2,2,this.index];
            // hipsTransform.rotation = Quaternion.FromToRotation(-Vector3.right, p2-p1);
            // UpdateBoneRotate(HumanBodyBones.Chest, 13, 14, -hipsTransform.right);

            // 背骨の更新
            UpdateBoneRotate(HumanBodyBones.Neck, 9, 12, hipsTransform.up);
            UpdateBoneRotate(HumanBodyBones.Spine, 6, 3, -hipsTransform.up);

            // 左足の更新
            UpdateBoneRotate(HumanBodyBones.LeftUpperLeg, 2, 5, -hipsTransform.up);
            UpdateBoneRotate(HumanBodyBones.LeftLowerLeg, 5, 8, -hipsTransform.up);
            UpdateBoneRotate(HumanBodyBones.LeftFoot, 8, 11, hipsTransform.forward);

            // 右足の更新
            UpdateBoneRotate(HumanBodyBones.RightUpperLeg, 1, 4, -hipsTransform.up);
            UpdateBoneRotate(HumanBodyBones.RightLowerLeg, 4, 7, -hipsTransform.up);
            UpdateBoneRotate(HumanBodyBones.RightFoot, 7, 10, hipsTransform.forward);

            // 左手の更新
            UpdateBoneRotate(HumanBodyBones.LeftShoulder, 14, 17, -hipsTransform.right);
            UpdateBoneRotate(HumanBodyBones.LeftUpperArm, 17, 19, -hipsTransform.right);
            UpdateBoneRotate(HumanBodyBones.LeftLowerArm, 19, 21, -hipsTransform.right);

            // 右手の更新
            UpdateBoneRotate(HumanBodyBones.RightShoulder, 13, 16, hipsTransform.right);
            UpdateBoneRotate(HumanBodyBones.RightUpperArm, 16, 18, hipsTransform.right);
            UpdateBoneRotate(HumanBodyBones.RightLowerArm, 18, 20, hipsTransform.right);
        }

        // ボーン向きの更新
        void UpdateBoneRotate(HumanBodyBones bone, int p1, int p2, Vector3 axis)
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
        }
    }
}
