mergeInto(LibraryManager.library, {

  WXBridge_Init: function (gameObjectPtr) {
    var gameObjectName = UTF8ToString(gameObjectPtr);
    console.log("[WXBridge:JS] WXBridge_Init called — gameObjectName=" + gameObjectName + ", typeof wx=" + typeof wx);
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

    // Register helper functions on window so they are accessible from all bridge functions.
    // This avoids Emscripten scope isolation issues with mergeInto.
    if (!window.__wxBridgeHelpers) {
      window.__wxBridgeHelpers = {

        sendToUnity: function (state, method, payload) {
          if (!state || !state.unityGameObject || typeof SendMessage !== "function") {
            return;
          }
          var value = payload == null ? "" : payload;
          SendMessage(state.unityGameObject, method, value);
        },

        stringifyError: function (e) {
          if (!e) return "unknown";
          try {
            return JSON.stringify(e);
          } catch (ex) {
            return String(e);
          }
        },

        ensureRewardedAd: function (state) {
          if (!state.rewardedAdUnitId || typeof wx === "undefined" || !wx.createRewardedVideoAd) {
            return null;
          }
          if (state.rewardedAd) {
            return state.rewardedAd;
          }

          var helpers = window.__wxBridgeHelpers;
          var ad = wx.createRewardedVideoAd({
            adUnitId: state.rewardedAdUnitId
          });

          ad.onError(function (adErr) {
            helpers.sendToUnity(state, "OnRewardedAdError", helpers.stringifyError(adErr));
          });

          ad.onClose(function (result) {
            var isEnded = true;
            if (result && typeof result.isEnded !== "undefined") {
              isEnded = result.isEnded === true;
            }
            helpers.sendToUnity(state, "OnRewardedAdClosed", isEnded ? "1" : "0");
          });

          state.rewardedAd = ad;
          return ad;
        },

        ensureBannerAd: function (state) {
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
        },

        ensureInterstitialAd: function (state) {
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
      };
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
    var helpers = window.__wxBridgeHelpers;
    if (!helpers) return;

    var ad = helpers.ensureRewardedAd(state);
    if (!ad || !ad.load) return;

    ad.load().catch(function () {});
  },

  WXBridge_ShowRewardedAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    var helpers = window.__wxBridgeHelpers;
    if (!state || typeof wx === "undefined" || !state.rewardedAdUnitId || !helpers) {
      if (helpers) helpers.sendToUnity(state, "OnRewardedAdClosed", "0");
      return;
    }

    var ad = helpers.ensureRewardedAd(state);
    if (!ad || !ad.show) {
      helpers.sendToUnity(state, "OnRewardedAdClosed", "0");
      return;
    }

    var showImpl = function () {
      ad.show().catch(function (showErr) {
        helpers.sendToUnity(state, "OnRewardedAdError", helpers.stringifyError(showErr));
        helpers.sendToUnity(state, "OnRewardedAdClosed", "0");
      });
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

  WXBridge_ShowBannerAd: function () {
    var state = window.MiniGameTemplateWXBridge;
    var helpers = window.__wxBridgeHelpers;
    if (!state || typeof wx === "undefined" || !state.bannerAdUnitId || !helpers) return;

    var ad = helpers.ensureBannerAd(state);
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
    var helpers = window.__wxBridgeHelpers;
    if (!state || typeof wx === "undefined" || !state.interstitialAdUnitId || !helpers) return;

    var ad = helpers.ensureInterstitialAd(state);
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
  },

  WXBridge_CheckPrivacy: function () {
    var state = window.MiniGameTemplateWXBridge;
    var helpers = window.__wxBridgeHelpers;
    if (!state || typeof wx === "undefined" || !wx.getPrivacySetting) {
      if (helpers) helpers.sendToUnity(state, "OnPrivacyCheckResult", "0");
      return;
    }

    wx.getPrivacySetting({
      success: function (res) {
        var needAuth = res.needAuthorization ? "1" : "0";
        helpers.sendToUnity(state, "OnPrivacyCheckResult", needAuth);
      },
      fail: function () {
        helpers.sendToUnity(state, "OnPrivacyCheckResult", "0");
      }
    });
  },

  WXBridge_RequirePrivacy: function () {
    var state = window.MiniGameTemplateWXBridge;
    var helpers = window.__wxBridgeHelpers;
    if (!state || typeof wx === "undefined" || !wx.requirePrivacyAuthorize) {
      if (helpers) helpers.sendToUnity(state, "OnPrivacyRequireResult", "1");
      return;
    }

    wx.requirePrivacyAuthorize({
      success: function () {
        helpers.sendToUnity(state, "OnPrivacyRequireResult", "1");
      },
      fail: function () {
        helpers.sendToUnity(state, "OnPrivacyRequireResult", "0");
      }
    });
  },

  // === V2: Cloud Function Bridge (SG_TDD_06 §5.1) ===

  WXBridge_InitCloud: function (envIdPtr) {
    console.log("[WXBridge:JS] WXBridge_InitCloud called");
    if (typeof wx === "undefined" || !wx.cloud) {
      console.error("[WXBridge:JS] InitCloud FAILED — wx or wx.cloud is undefined. typeof wx=" + typeof wx);
      return;
    }

    var envId = UTF8ToString(envIdPtr);
    console.log("[WXBridge:JS] InitCloud envId=" + (envId || "(empty, using default)"));
    if (!envId || envId === "") {
      wx.cloud.init();
    } else {
      wx.cloud.init({ env: envId });
    }

    if (!window.MiniGameTemplateWXBridge) {
      window.MiniGameTemplateWXBridge = { unityGameObject: "" };
    }
    window.MiniGameTemplateWXBridge.cloudInitialized = true;
    console.log("[WXBridge:JS] InitCloud SUCCESS — cloudInitialized=true");
  },

  WXBridge_CallCloudFunction: function (requestId, namePtr, dataPtr) {
    var state = window.MiniGameTemplateWXBridge;
    var helpers = window.__wxBridgeHelpers;
    console.log("[WXBridge:JS] CallCloudFunction called — requestId=" + requestId + ", state=" + !!state + ", wx=" + (typeof wx) + ", wx.cloud=" + !!(typeof wx !== "undefined" && wx.cloud));
    if (!state || typeof wx === "undefined" || !wx.cloud) {
      console.error("[WXBridge:JS] CallCloudFunction ABORT — missing state/wx/wx.cloud");
      if (helpers) {
        helpers.sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({
          success: false,
          requestId: requestId,
          name: "",
          error: "no wx.cloud"
        }));
      }
      return;
    }

    var name = UTF8ToString(namePtr);
    var data = UTF8ToString(dataPtr);
    var parsedData = {};
    try { parsedData = JSON.parse(data); } catch (e) {}

    // Guard: ensure cloud was initialized before calling
    if (!state.cloudInitialized) {
      console.error("[WXBridge:JS] CallCloudFunction ABORT — cloudInitialized=false! Did WXBridge_InitCloud succeed?");
      if (helpers) {
        helpers.sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({
          success: false,
          requestId: requestId,
          name: name,
          error: "cloud not initialized"
        }));
      }
      return;
    }

    console.log("[WXBridge:JS] CallCloudFunction invoking wx.cloud.callFunction — name=" + name + ", data=" + data);

    var CLOUD_TIMEOUT_MS = 15000;
    var timeoutId = setTimeout(function () {
      timeoutId = null;
      console.error("[WXBridge:JS] CallCloudFunction TIMEOUT (" + CLOUD_TIMEOUT_MS + "ms) — name=" + name + ". Cloud function cold start may need longer, or the function does not exist.");
      if (helpers) {
        helpers.sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({
          success: false,
          requestId: requestId,
          name: name,
          error: "timeout: " + CLOUD_TIMEOUT_MS + "ms exceeded"
        }));
      }
    }, CLOUD_TIMEOUT_MS);

    wx.cloud.callFunction({
      name: name,
      data: parsedData,
      success: function (res) {
        if (timeoutId === null) return;
        clearTimeout(timeoutId);
        console.log("[WXBridge:JS] CallCloudFunction SUCCESS — name=" + name + ", result=" + JSON.stringify(res.result));
        if (helpers) {
          helpers.sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({
            success: true,
            requestId: requestId,
            name: name,
            result: JSON.stringify(res.result)
          }));
        }
      },
      fail: function (failRes) {
        if (timeoutId === null) return;
        clearTimeout(timeoutId);
        console.error("[WXBridge:JS] CallCloudFunction FAIL — name=" + name + ", err=" + JSON.stringify(failRes));
        if (helpers) {
          helpers.sendToUnity(state, "OnCloudFunctionResult", JSON.stringify({
            success: false,
            requestId: requestId,
            name: name,
            error: helpers.stringifyError(failRes)
          }));
        }
      }
    });
  }
});
