# 流程 A：生成 UI 示意图（仅图片）

> 归档自 SKILL.md，按需加载。

## 步骤

1. **分析用户输入**（效果图或文字描述）
   - 识别所有 UI 元素：按钮、文本、图标、列表、滚动区域等
   - 推断元素用途和交互逻辑
   - 确定布局层级和层叠关系

2. **用 HTML/CSS 渲染示意图**
   - 创建一个独立的 HTML 文件
   - 使用 CSS 盒模型精确还原布局
   - 白模风格：深色背景 + 浅色/彩色色块表示不同元素类型
   - 添加文字标注说明各区域用途

3. **截图保存**
   - 使用 Puppeteer 将 HTML 页面截图保存为 JPG/PNG
   - 返回图片给用户确认

4. **按需生成原则**
   - ⚠️ **此流程不生成任何 FairyGUI XML 文件**
   - 仅输出示意图图片

## HTML 示意图模板

```html
<!DOCTYPE html>
<html>
<head>
<meta charset="utf-8">
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { background: #1a1a2e; font-family: Arial, sans-serif; }
  .container { width: [宽]px; height: [高]px; position: relative; overflow: hidden; }
  /* 用不同颜色区分元素类型 */
  .btn { background: #4a90d9; border-radius: 8px; }
  .text { color: #ffffff; }
  .icon { background: #666; border-radius: 50%; }
  .panel { background: #2d2d44; border-radius: 4px; }
  .input { background: #222; border: 1px solid #999; }
</style>
</head>
<body>
  <div class="container">
    <!-- 按层级排列 UI 元素 -->
  </div>
</body>
</html>
```
