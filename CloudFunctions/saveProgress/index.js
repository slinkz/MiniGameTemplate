// cloudfunctions/saveProgress/index.js
// WeChat Cloud Function: save player progress (V3 — cloud-authoritative, direct overwrite)
// No union merge. Client sends the complete current state; cloud stores it as-is.
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();
    const { clearedLevels, version, clientVersion } = event;

    const data = {
        version: version || 2,
        clearedLevels: clearedLevels || [],
        lastSyncTime: Date.now(),
        clientVersion: clientVersion || "unknown"
    };

    try {
        await db.collection('progress').doc(OPENID).set({ data });
        return { success: true, data };
    } catch (e) {
        return { success: false, error: e.errMsg };
    }
};
