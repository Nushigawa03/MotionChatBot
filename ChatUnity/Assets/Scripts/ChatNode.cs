using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace MotionChat
{
    public class ChatNode : MonoBehaviour
    {
        [SerializeField] TMP_Text chatText;
        public void init(string text)
        {
            chatText.text = text;
        }
    }
}