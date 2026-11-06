# GameplayUpdater

> 归档日期：2026-10-30

## 概述
类似DOTS的EntitiySystem可以指定在哪个UpdateGroup中执行,同个Group中按属性指定的优先级排序,迁移到当前框架下就是Entity的UpdateSystem里指定UpdateGroup和优先级

---

## 功能
### UpdateGroup和优先级的组织形式
DOTS的EntitiySystem是在EntitiySystem处理Entity,EntitySystem变动很小,所以只需要排序EntitySystem就可以
当前框架是处理Entity,直接排序Entity消耗太大,所以用UpdateGroup,优先级,Entity类型进行分桶排序,桶里面是EntityQueue,
先以UpdateGroup分桶以Update顺序进行排序,updateGroup桶内以Entity类型分桶,以优先级进行排序,Entity类型桶内是EntityRef的Queue
UpdateGroup可以提前扫描类型全部创建,最好支持动态添加

#### UpdateGroup动态添加
UpdateGroup的添加延迟到帧尾进行

### Entity在Updater里的注册和销毁
注册到Updater内对应桶内, EntityRef的Queue在遍历时检查Ref,合法就重新入队,否则跳过

## 单元测试
测试Entity的注册, 更新, 销毁的功能都正确性
### 注册
只注册
更新时注册
循环内注册

### 更新
只更新
更新时注册
更新时注销

### 注销
只注销
更新时注销
循环内注销

## 压力测试
### 更新

### 更新时注册

### 更新时注销

### 更新+更新时注册+更新时注销


## 问题
