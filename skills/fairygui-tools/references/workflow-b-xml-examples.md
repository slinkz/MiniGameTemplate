# 流程 B：FairyGUI 工程文件生成示例

> 归档自 SKILL.md，按需加载。

## 输出文件结构

```
输出目录/
├── package.xml          # 包描述
├── Main.xml             # 主界面（或用户指定名称）
├── components/          # 子组件目录
│   ├── Button1.xml
│   ├── ListItem.xml
│   └── ...
└── images/              # 空目录（预留给美术替换）
```

## 完整生成示例：简单弹窗

### package.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<packageDescription id="ab12cd34">
  <resources>
    <component id="gen_01" name="SimpleDialog.xml" path="/" exported="true"/>
    <component id="gen_02" name="ConfirmButton.xml" path="/components/"/>
  </resources>
  <publish name="MyUI" genCode="true">
    <atlas name="Default" index="0"/>
  </publish>
</packageDescription>
```

### SimpleDialog.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<component size="400,300">
  <displayList>
    <graph id="gen_03" name="bg" xy="0,0" size="400,300"
           type="rect" fillColor="#ff2d2d44" corner="12"/>
    <text id="gen_04" name="title" xy="20,15" size="360,30"
          fontSize="24" color="#ffffff" bold="true"
          align="center" autoSize="none" text="Dialog Title"/>
    <graph id="gen_05" name="divider" xy="20,55" size="360,1"
           type="rect" fillColor="#ff555555"/>
    <text id="gen_06" name="content" xy="20,70" size="360,150"
          fontSize="18" color="#cccccc" autoSize="height"
          text="Dialog content goes here."/>
    <component id="gen_07" name="confirmBtn" src="gen_02"
               fileName="components/ConfirmButton.xml"
               xy="140,240" size="120,40">
      <Button title="OK"/>
    </component>
  </displayList>
</component>
```

### components/ConfirmButton.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<component size="120,40" extention="Button">
  <controller name="button" pages="0,up,1,down,2,over,3,selectedOver" selected="0"/>
  <displayList>
    <graph id="gen_08" name="bg_up" xy="0,0" size="120,40"
           type="rect" fillColor="#ff4a90d9" corner="8">
      <gearDisplay controller="button" pages="0"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <graph id="gen_09" name="bg_down" xy="0,0" size="120,40"
           type="rect" fillColor="#ff3a7bc8" corner="8">
      <gearDisplay controller="button" pages="1,3"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <graph id="gen_10" name="bg_over" xy="0,0" size="120,40"
           type="rect" fillColor="#ff5aa0e9" corner="8">
      <gearDisplay controller="button" pages="2"/>
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <text id="gen_11" name="title" xy="0,0" size="120,40"
          fontSize="18" color="#ffffff" align="center" vAlign="middle"
          autoSize="none" singleLine="true" text="">
      <relation target="" sidePair="width-width,height-height"/>
    </text>
  </displayList>
  <Button/>
</component>
```

## ProgressBar 白模正确写法（三步走）

### 第一步 — 创建独立组件文件 `components/HPBar.xml`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<component size="718,12" extention="ProgressBar">
  <displayList>
    <graph id="gen_30" name="bg" xy="0,0" size="718,12"
           type="rect" fillColor="#ff444444" corner="6">
      <relation target="" sidePair="width-width,height-height"/>
    </graph>
    <graph id="gen_31" name="bar" xy="0,0" size="718,12"
           type="rect" fillColor="#ff4caf50" corner="6"/>
  </displayList>
  <ProgressBar titleType="percent"/>
</component>
```

### 第二步 — 在 `package.xml` 中声明：

```xml
<component id="gen_05" name="HPBar.xml" path="/components/"/>
```

### 第三步 — 在父组件 displayList 中用 `<component src>` 引用：

```xml
<component id="gen_13" name="hp_bar" src="gen_05"
           fileName="components/HPBar.xml" xy="16,1260" size="718,12">
  <ProgressBar value="100" max="100"/>
</component>
```

**关键要点**：
- `extention="ProgressBar"` 声明在独立组件的 `<component>` 根标签上
- `<ProgressBar/>` 扩展定义元素放在 `</displayList>` **之后**（组件定义中）
- `<ProgressBar value="..." max="..."/>` 实例化参数放在引用处（displayList 中的 component 内）
- `name="bar"` 是 ProgressBar 识别填充元素的**强制命名约定**，不可改名
- 独立组件内部自带背景 graph，无需在父组件额外放一层背景

## GGraph 变色双层方案示例

```xml
<controller name="state" pages="0,off,1,on" selected="0"/>
<displayList>
  <!-- 暗色层（state=off 时显示） -->
  <graph id="g1" name="star_off" xy="0,0" size="12,12" type="eclipse"
         fillColor="#ff555555">
    <gearDisplay controller="state" pages="0"/>
  </graph>
  <!-- 亮色层（state=on 时显示） -->
  <graph id="g2" name="star_on" xy="0,0" size="12,12" type="eclipse"
         fillColor="#ffffc107">
    <gearDisplay controller="state" pages="1"/>
  </graph>
</displayList>
```
