using UnityEngine;

namespace Core.Services.Data
{
    [CreateAssetMenu(menuName = "Create RecordStorage", fileName = "RecordStorage", order = 0)]
    public class RecordStorage : ScriptableObject
    {
        public RecordSO[] Records;
    }
}