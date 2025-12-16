using System;
using System.Text;
using UnityEngine;

namespace NetCodeTest.Lobby
{
    [Serializable]
    public sealed class LobbyJoinPayload
    {
        public int v = 1;
        public string userId;
        public string code;
        public string password;

        public static byte[] Encode(string userId, string code, string password)
        {
            var p = new LobbyJoinPayload
            {
                v = 1,
                userId = userId ?? "",
                code = code ?? "",
                password = password ?? ""
            };
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(p));
        }

        public static LobbyJoinPayload Decode(byte[] payload)
        {
            // Backward compatible with old string userId
            if (payload == null || payload.Length == 0)
                return new LobbyJoinPayload { userId = "unknown" };

            string s;
            try { s = Encoding.UTF8.GetString(payload); }
            catch { return new LobbyJoinPayload { userId = "invalid" }; }

            if (string.IsNullOrWhiteSpace(s))
                return new LobbyJoinPayload { userId = "unknown" };

            s = s.Trim();

            // JSON
            if (s.Length > 0 && s[0] == '{')
            {
                try
                {
                    var p = JsonUtility.FromJson<LobbyJoinPayload>(s);
                    if (p == null) return new LobbyJoinPayload { userId = "invalid" };
                    if (string.IsNullOrWhiteSpace(p.userId)) p.userId = "unknown";
                    if (p.code == null) p.code = "";
                    if (p.password == null) p.password = "";
                    return p;
                }
                catch
                {
                    return new LobbyJoinPayload { userId = "invalid" };
                }
            }

            // legacy string = userId
            return new LobbyJoinPayload { userId = s, code = "", password = "" };
        }
    }
}
