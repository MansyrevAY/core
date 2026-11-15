using UnityEngine;

namespace Core.Services.Data
{
    [CreateAssetMenu(menuName = "Create RecordSO", fileName = "RecordSO", order = 0)]
    public class RecordSO : ScriptableObject
    {
        [SerializeReference, SerializeField]
        public BaseRecord Record;
    }
}