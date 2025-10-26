using UnityEngine;

namespace Ludo
{
    public interface ITokenProvider
    {
        Transform GetTokenTransform(int playerIndex, int tokenOrdinal);
        void HighlightToken(int playerIndex, int tokenOrdinal, bool highlight);
        void MoveTokenToPosition(int playerIndex, int tokenOrdinal, Vector3 position);
    }
}