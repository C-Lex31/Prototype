using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace Ace.Utility
{
    /// <summary>Manages onscreen positions for Cinemachine debugging output</summary>
    public class ACEDebug
    {
        static HashSet<Object> mClients;

        /// <summary>Release a screen rectangle previously obtained through GetScreenPos()</summary>
        /// <param name="client">The client caller.  Used as a handle.</param>
        public static void ReleaseScreenPos(Object client)
        {
            if (mClients != null && mClients.Contains(client))
                mClients.Remove(client);
        }

        /// <summary>Reserve an on-screen rectangle for debugging output.</summary>
        /// <param name="client">The client caller.  This is used as a handle.</param>
        /// <param name="text">Sample text, for determining rectangle size</param>
        /// <param name="style">What style will be used to draw, used here for
        /// determining rect size</param>
        /// <returns>An area on the game screen large enough to print the text
        /// in the style indicated</returns>
        public static Rect GetScreenPos(Object client, string text, GUIStyle style)
        {
            if (mClients == null)
                mClients = new HashSet<Object>();
            if (!mClients.Contains(client))
                mClients.Add(client);

            Vector2 pos = new Vector2(0, 0);
            Vector2 size = style.CalcSize(new GUIContent(text));
            if (mClients != null)
            {
                foreach (var c in mClients)
                {
                    if (c == client)
                        break;
                    pos.y += size.y;
                }
            }
            return new Rect(pos, size);
        }

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public delegate void OnGUIDelegate();

        /// <summary>
        /// Delegate for OnGUI debugging.  
        /// This will be called by the CinemachineBrain in its OnGUI (editor only)
        /// </summary>
        public static OnGUIDelegate OnGUIHandlers;

        private static List<StringBuilder> mAvailableStringBuilders;

        /// <summary>Get a preallocated StringBuilder from the pool</summary>
        public static StringBuilder SBFromPool()
        {
            if (mAvailableStringBuilders == null || mAvailableStringBuilders.Count == 0)
                return new StringBuilder();
            var sb = mAvailableStringBuilders[mAvailableStringBuilders.Count - 1];
            mAvailableStringBuilders.RemoveAt(mAvailableStringBuilders.Count - 1);
            sb.Length = 0;
            return sb;
        }

        /// <summary>Return a StringBuilder to the preallocated pool</summary>
        public static void ReturnToPool(StringBuilder sb)
        {
            if (mAvailableStringBuilders == null)
                mAvailableStringBuilders = new List<StringBuilder>();
            mAvailableStringBuilders.Add(sb);
        }
    }
}
