using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace MotionChat
{
    public class ChatView : MonoBehaviour
    {
        [SerializeField] GameObject content;
        [SerializeField] GameObject chatNodePrefab;
        [SerializeField] TMP_InputField inputField;
        [SerializeField] TMP_Text MotionPrompt;

        public async Task<ChatMotionReactionModel> SendGPT(APIClient _APIClient)
        {
            string user_message = inputField.text;
            inputField.text = string.Empty;
            SendChatView("User:" + user_message);

            var chatMotionReaction = await _APIClient.Chat(user_message);
            if (chatMotionReaction is not null)
            {
                SendChatView("ChatGPT:" + chatMotionReaction.message);
                SendPromptView(chatMotionReaction.motion);
            }
            else
            {
                SendChatView("返答エラーが起きました。");
            }
            return chatMotionReaction;
        }
        void SendChatView(string text)
        {
            var chatNode = Instantiate<GameObject>(chatNodePrefab, content.transform, false);
            chatNode.GetComponent<ChatNode>().init(text);
        }
        void SendPromptView(string text)
        {
            MotionPrompt.text = text;
        }
    }
}