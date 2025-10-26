using Ludo;
using UnityEngine;

namespace Placements.Runtime
{
    public class UnityPositionProvider : IPositionProvider
    {
        private readonly TokenBase[] _bases;
        private readonly Tiles _tiles;
        private readonly ColorTiles[] _homeStretches;
        private readonly HomeBase[] _homes;

        public UnityPositionProvider(TokenBase[] bases, Tiles tiles, ColorTiles[] homeStretches, HomeBase[] homes)
        {
            _bases = bases;
            _tiles = tiles;
            _homeStretches = homeStretches;
            _homes = homes;
        }

        public Vector3 GetBasePosition(int playerIndex, int tokenOrdinal)
        {
            if (playerIndex < 0 || playerIndex >= _bases.Length) return Vector3.zero;
            var baseObj = _bases[playerIndex];
            if (tokenOrdinal < 0 || tokenOrdinal >= baseObj.BasePositions.Length) return Vector3.zero;
            return baseObj.BasePositions[tokenOrdinal];
        }

        public Vector3 GetMainTrackPosition(int absPosition)
        {
            return _tiles.GetMainTilePosition(absPosition);
        }

        public Vector3 GetHomeStretchPosition(int playerIndex, int step)
        {
            if (playerIndex < 0 || playerIndex >= _homeStretches.Length) return Vector3.zero;
            var stretch = _homeStretches[playerIndex].tileFinal;
            if (step < 0 || step >= stretch.Length) return Vector3.zero;
            return stretch[step].transform.position;
        }

        public Vector3 GetHomePosition(int playerIndex, int tokenOrdinal)
        {
            if (playerIndex < 0 || playerIndex >= _homes.Length) return Vector3.zero;
            var home = _homes[playerIndex];
            if (tokenOrdinal < 0 || tokenOrdinal >= home.homePositions.Length) return Vector3.zero;
            return home.homePositions[tokenOrdinal].position;
        }
    }
}