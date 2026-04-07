mergeInto(LibraryManager.library, {
  WXBridge_Init: function (gameObjectPtr) {
    var gameObjectName = UTF8ToString(gameObjectPtr);
    if (!window.MiniGameTemplateWXBridge) {
      window.MiniGameTemplateWXBridge = {
        unityGameObject: gameObjectName,
        rewardedAdUnitId: "",
        bannerAdUnitId: "",
        interstitialAdUnitId: "",
        rewardedAd: null,
        bannerAd: null,
        interstitialAd: null,
        bannerResizeBound: false
      };
    } else {
      window.MiniGameTemplateWXBridge.unityGameObject = gameObjectName;
    }
  },

  WXBridge_IsWeChatEnv: function () {
    if (typeof wx === "undefined") return 0;
    return 1;
  },

  WXBridge_SetAdUnitIds: function (rewardedPtr, bannerPtr, interstitialPtr) {
    if (!window.MiniGameTemplateWXBridge) return;

    var state = window.MiniGameTemplateWXBridge;
    var rewarded = UTF8ToString(rewardedPtr);
    var banner = UTF8ToString(bannerPtr);
    var interstitial = UTF8ToString(interstitialPtr);

    if (state.rewardedAdUnitId !== rewarded && state.rewardedAd && state.rewardedAd.destroy) {
      state.rewardedAd.destroy();
      state.rewardedAd = null;
    }

    if (state.bannerAdUnitId !== banner && state.bannerAd && state.bannerAd.destroy) {
      state.bannerAd.destroy();
      state.bannerAd = null;
    }

    if (state.interstitialAdUnitId !== interstitial && state.interstitialAd && state.interstitialAd.destroy) {
      state.interstitialAd.destroy();
      state.interstitialAd = null;
    }

    state.rewardedAdUnitId = rewarded;
    state.bannerAdUnitId = banner;
    state.interstitialAdUnitId = interstitial;
  },

  WXBridge_PreloadRewardedAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || typeof wx === "undefined" || !state.rewardedAdUnitId) return;

    var ad = ensureRewardedAd(state);
    if (!ad || !ad.load) return;

    ad.load().catch(function () {});
  },

  WXBridge_ShowRewardedAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || typeof wx === "undefined" || !state.rewardedAdUnitId) {
      sendToUnity(state, "OnRewardedAdClosed", "0");
      return;
    }

    var ad = ensureRewardedAd(state);
    if (!ad || !ad.show) {
      sendToUnity(state, "OnRewardedAdClosed", "0");
      return;
    }

    var showImpl = function () {
      ad.show().catch(function (err) {
        sendToUnity(state, "OnRewardedAdError", stringifyError(err));
        sendToUnity(state, "OnRewardedAdClosed", "0");
      });
    };

    if (ad.load) {
      ad.load().then(function () {
        showImpl();
      }).catch(function () {
        // 某些机型下 load 失败后仍可直接 show，失败结果由 showImpl 统一回调。
        showImpl();
      });
      return;
    }


    showImpl();
  },

  WXBridge_ShowBannerAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || typeof wx === "undefined" || !state.bannerAdUnitId) return;

    var ad = ensureBannerAd(state);
    if (!ad || !ad.show) return;

    ad.show().catch(function () {});
  },

  WXBridge_HideBannerAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || !state.bannerAd || !state.bannerAd.hide) return;

    state.bannerAd.hide();
  },

  WXBridge_ShowInterstitialAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    if (!state || typeof wx === "undefined" || !state.interstitialAdUnitId) return;

    var ad = ensureInterstitialAd(state);
    if (!ad || !ad.show) return;

    var showImpl = function () {
      ad.show().catch(function () {});
    };

    if (ad.load) {
      ad.load().then(function () {
        showImpl();
      }).catch(function () {
        showImpl();
      });
      return;
    }

    showImpl();
  },

  WXBridge_Share: function (titlePtr, imageUrlPtr, queryPtr) {
    if (typeof wx === "undefined" || !wx.shareAppMessage) return;

    var title = UTF8ToString(titlePtr);
    var imageUrl = UTF8ToString(imageUrlPtr);
    var query = UTF8ToString(queryPtr);

    wx.shareAppMessage({
      title: title,
      imageUrl: imageUrl,
      query: query
    });
  },

  WXBridge_Vibrate: function (isLong) {
    if (typeof wx === "undefined") return;

    if (isLong === 1 && wx.vibrateLong) {
      wx.vibrateLong();
      return;
    }

    if (wx.vibrateShort) {
      wx.vibrateShort();
    }
  }
});

function ensureRewardedAd(state) {
  if (!state.rewardedAdUnitId || typeof wx === "undefined" || !wx.createRewardedVideoAd) {
    return null;
  }

  if (state.rewardedAd) {
    return state.rewardedAd;
  }

  var ad = wx.createRewardedVideoAd({
    adUnitId: state.rewardedAdUnitId
  });

  ad.onError(function (err) {
    sendToUnity(state, "OnRewardedAdError", stringifyError(err));
  });

  ad.onClose(function (result) {
    var isEnded = true;
    if (result && typeof result.isEnded !== "undefined") {
      isEnded = result.isEnded === true;
    }
    sendToUnity(state, "OnRewardedAdClosed", isEnded ? "1" : "0");
  });

  state.rewardedAd = ad;
  return ad;
}

function ensureBannerAd(state) {
  if (!state.bannerAdUnitId || typeof wx === "undefined" || !wx.createBannerAd) {
    return null;
  }

  if (state.bannerAd) {
    return state.bannerAd;
  }

  var systemInfo = wx.getSystemInfoSync ? wx.getSystemInfoSync() : { windowWidth: 320, windowHeight: 568 };
  var width = Math.min(320, systemInfo.windowWidth || 320);

  var ad = wx.createBannerAd({
    adUnitId: state.bannerAdUnitId,
    adIntervals: 30,
    style: {
      left: ((systemInfo.windowWidth || width) - width) / 2,
      top: (systemInfo.windowHeight || 568) - 110,
      width: width
    }
  });

  ad.onResize(function (size) {
    if (!ad.style) return;

    var latestInfo = wx.getSystemInfoSync ? wx.getSystemInfoSync() : systemInfo;
    ad.style.left = ((latestInfo.windowWidth || size.width) - size.width) / 2;
    ad.style.top = (latestInfo.windowHeight || 568) - size.height;
  });

  state.bannerAd = ad;
  return ad;
}

function ensureInterstitialAd(state) {
  if (!state.interstitialAdUnitId || typeof wx === "undefined" || !wx.createInterstitialAd) {
    return null;
  }

  if (state.interstitialAd) {
    return state.interstitialAd;
  }

  var ad = wx.createInterstitialAd({
    adUnitId: state.interstitialAdUnitId
  });

  state.interstitialAd = ad;
  return ad;
}

function sendToUnity(state, method, payload) {
  if (!state || !state.unityGameObject || typeof SendMessage !== "function") {
    return;
  }

  var value = payload == null ? "" : payload;
  SendMessage(state.unityGameObject, method, value);
}

function stringifyError(err) {
  if (!err) return "unknown";
  try {
    return JSON.stringify(err);
  } catch (e) {
    return String(err);
  }
}
