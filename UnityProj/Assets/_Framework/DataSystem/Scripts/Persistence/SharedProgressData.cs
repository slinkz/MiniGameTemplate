using System;
using System.Collections.Generic;

namespace MiniGameTemplate.Data
{
    /// <summary>
    /// V2 progress data DTO (shared type — SG_TDD_06 §3.3).
    /// Used by both CloudSyncService (merge) and SG_ProgressManager (Load/Save).
    /// 
    /// v0.4: Promoted from SG_ProgressManager's internal private class to shared type.
    /// Field names must match JSON keys exactly for JsonUtility.
    /// </summary>
    [Serializable]
    public class SharedProgressData
    {
        public int version = 1;
        public List<int> clearedLevels = new List<int>();
    }
}
