// cloudfunctions/saveProgress/index.js
// WeChat Cloud Function: save player progress (V3 — cloud-authoritative, direct overwrite)
// No union merge. Client sends the complete current state; cloud stores it as-is.
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();

    // V3: Client sends the complete SharedProgressData JSON.
    // Store it as-is (cloud-authoritative, direct overwrite).
    // Expected fields: version, clearedLevels, levelStars,
    //   unlockedSkillIds, unlockedPassiveIds, totalDeaths,
    //   maxKillsInOneLevel, totalHitsTaken, clientVersion.
    const {
        version,
        clearedLevels,
        levelStars,
        unlockedSkillIds,
        unlockedPassiveIds,
        totalDeaths,
        maxKillsInOneLevel,
        totalHitsTaken,
        clientVersion,
    } = event;

    const data = {
        version: version || 3,
        clearedLevels: clearedLevels || [],
        levelStars: levelStars || [],
        unlockedSkillIds: unlockedSkillIds || [],
        unlockedPassiveIds: unlockedPassiveIds || [],
        totalDeaths: totalDeaths || 0,
        maxKillsInOneLevel: maxKillsInOneLevel || 0,
        totalHitsTaken: totalHitsTaken || 0,
        clientVersion: clientVersion || 'unknown',
        lastSyncTime: Date.now(),
    };

    try {
        await db.collection('progress').doc(OPENID).set({ data });
        return { success: true, data };
    } catch (e) {
        return { success: false, error: e.errMsg };
    }
};
