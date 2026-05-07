// cloudfunctions/saveProgress/index.js
// WeChat Cloud Function: save player progress with server-side union merge (SG_TDD_06 §3.2.3)
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();
    const { clearedLevels, version, clientVersion } = event;

    // Server-side union merge: read existing → merge → write back
    let existingLevels = [];
    try {
        const existing = await db.collection('progress').doc(OPENID).get();
        existingLevels = existing.data.clearedLevels || [];
    } catch (e) {
        // Document doesn't exist yet — empty array
    }

    // Union
    const merged = [...new Set([...existingLevels, ...(clearedLevels || [])])].sort((a, b) => a - b);

    const data = {
        version: version || 2,
        clearedLevels: merged,
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
