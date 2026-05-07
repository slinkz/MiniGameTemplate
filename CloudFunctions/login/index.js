// cloudfunctions/login/index.js
// WeChat Cloud Function: silent login (SG_TDD_06 §3.2.1)
const cloud = require('wx-server-sdk');
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV });

exports.main = async (event, context) => {
    const { OPENID } = cloud.getWXContext();

    // V2 simplified: cloud-dev environment guarantees OPENID authenticity.
    // No custom token needed — openid IS the trust credential.
    return {
        openid: OPENID,
        token: OPENID, // V2: cloud-dev trust chain, openid = credential
        expireIn: 7200  // Suggest re-login after 2 hours
    };
};
