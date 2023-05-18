using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MotionChat
{
    public class Main : MonoBehaviour
    {
        APIClient _APIClient;
        ChatView _ChatView;
        Play _Play;

        void Start()
        {
            this._APIClient = new APIClient();
            this._ChatView = GetComponent<ChatView>();
            this._Play = GetComponent<Play>();
        }
        public async void OnClick()
        {
            string text_prompt;
            var chatMotionReaction = await _ChatView.SendGPT(_APIClient);

            text_prompt = chatMotionReaction.motion;

            _Play.difMotion = await APIClient.GetMotion(text_prompt); // [1,22,3,frame]
        }
    }
}