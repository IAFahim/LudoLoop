using UnityEngine;

namespace Ludo
{
    public interface IPositionProvider
    {
        Vector3 GetBasePosition(int playerIndex, int tokenOrdinal);
        Vector3 GetMainTrackPosition(int absPosition);
        Vector3 GetHomeStretchPosition(int playerIndex, int step);
        Vector3 GetHomePosition(int playerIndex, int tokenOrdinal);
    }
}