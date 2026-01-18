using UnityEngine;

namespace Gameplay.Coop
{
    [CreateAssetMenu(menuName = "Coop/Level Database", fileName = "LevelDatabase")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField] private LevelConfig[] levels;

        public LevelConfig[] Levels => levels;

        public LevelConfig GetLevel(int levelId)
        {
            if (levels == null) return null;

            foreach (var level in levels)
            {
                if (level != null && level.LevelId == levelId)
                    return level;
            }

            return null;
        }

        public bool TryGetLevel(int levelId, out LevelConfig config)
        {
            config = GetLevel(levelId);
            return config != null;
        }
    }
}
