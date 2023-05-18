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

        public async Task<ChatMotionReactionModel> SendGPT(APIClient _APIClient)
        {
            string user_message = inputField.text;
            inputField.text = string.Empty;
            SendView("User:" + user_message);

            var chatMotionReaction = await _APIClient.Chat(user_message);
            SendView("ChatGPT:" + chatMotionReaction.message);
            return chatMotionReaction;
        }
        void SendView(string text)
        {
            var chatNode = Instantiate<GameObject>(chatNodePrefab, content.transform, false);
            chatNode.GetComponent<ChatNode>().init(text);
        }
    }
}