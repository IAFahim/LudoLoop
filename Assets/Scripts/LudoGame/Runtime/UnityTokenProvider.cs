using Ludo;
using UnityEngine;

namespace LudoGame.Runtime
{
    public class UnityTokenProvider : ITokenProvider
    {
        private readonly TokenBase[] _bases;

        public UnityTokenProvider(TokenBase[] bases)
        {
            _bases = bases;
        }

        public Transform GetTokenTransform(int playerIndex, int tokenOrdinal)
        {
            if (playerIndex < 0 || playerIndex >= _bases.Length) return null;
            var baseObj = _bases[playerIndex];
            if (tokenOrdinal < 0 || tokenOrdinal >= baseObj.Tokens.Length) return null;
            return baseObj.Tokens[tokenOrdinal].transform;
        }

        public void HighlightToken(int playerIndex, int tokenOrdinal, bool highlight)
        {
            if (playerIndex < 0 || playerIndex >= _bases.Length) return;
            var baseObj = _bases[playerIndex];
            if (tokenOrdinal < 0 || tokenOrdinal >= baseObj.Tokens.Length) return;
            baseObj.Tokens[tokenOrdinal].SetHighlight(highlight);
        }

        public void MoveTokenToPosition(int playerIndex, int tokenOrdinal, Vector3 position)
        {
            var trans = GetTokenTransform(playerIndex, tokenOrdinal);
            if (trans != null) trans.position = position;
        }
    }
}