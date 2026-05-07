// cloudfunctions/getProgress/index.js
// WeChat Cloud Function: get player progress (SG_TDD_06 §3.2.2)
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });
const db = cloud.database();

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();

    try {
        const result = await db.collection('progress').doc(OPENID).get();
        return { success: true, data: result.data };
    } catch (e) {
        if (e.errCode === -1) {
            // Document not found = new user
            return { success: true, data: null };
        }
        return { success: false, error: e.errMsg };
    }
};
