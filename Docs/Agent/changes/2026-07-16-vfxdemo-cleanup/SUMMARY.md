---
system: knowledge-engineering
scope: vfxdemo-cleanup
status: active
created: 2026-07-16
related_docs: Docs/Agent/DEMO_CLEANUP_SOP.md, UnityProj/Assets/_Example/README.md
---

# VFXDemo Cleanup

## Motivation

Remove the standalone VFXDemo sample while preserving reusable VFX runtime capability and DanmakuDemo behavior.

## Decision Pattern

The user chose the decision-first workflow:

1. Agent lists decision questions.
2. Each question includes options, tradeoffs, and a recommendation.
3. User makes the final call.
4. Agent executes only after decisions are explicit.

## User Decisions

- Scope: remove only the standalone VFXDemo sample.
- Shared assets: migrate assets still used by DanmakuDemo.
- New asset home: `UnityProj/Assets/_Example/DanmakuDemo/VFX/`.
- Main menu: remove the VFXDemo button and keep the demo section.
- FairyGUI: update source XML, generated binding, and logic consistently.
- Docs: update current docs; keep changelog/history intact.
- VFX acceptance docs: remove VFXDemo and keep DanmakuDemo/business chain.
- Validation: run Unity batchmode if available.

## Result

VFXDemo scene, scripts, README, menu entry, Build Settings entry, and current docs references were removed. DanmakuDemo VFX dependencies were moved under DanmakuDemo while preserving GUIDs.
