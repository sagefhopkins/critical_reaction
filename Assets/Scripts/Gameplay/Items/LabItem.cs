using UnityEngine;

namespace Gameplay.Items
{
    [CreateAssetMenu(menuName = "Items", fileName = "LabItem")]
    public class LabItem : ScriptableObject
    {
    
        [SerializeField] private ushort id;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;

        public ushort Id => id;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
    }
}
