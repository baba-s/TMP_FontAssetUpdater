# TMP_FontAssetUpdater

You can automatically update FontAsset when the character you want to include in FontAsset of TextMesh Pro is changed.

[![](https://img.shields.io/github/release/baba-s/TMP_FontAssetUpdater.svg?label=latest%20version)](https://github.com/baba-s/TMP_FontAssetUpdater/releases)
[![](https://img.shields.io/github/release-date/baba-s/TMP_FontAssetUpdater.svg)](https://github.com/baba-s/TMP_FontAssetUpdater/releases)
![](https://img.shields.io/badge/Unity-2017.4%2B-red.svg)
![](https://img.shields.io/badge/.NET-3.5%2B-orange.svg)
[![](https://img.shields.io/github/license/baba-s/TMP_FontAssetUpdater.svg)](https://github.com/baba-s/TMP_FontAssetUpdater/blob/master/LICENSE)

## Features

- Support for Material Preset
- Mass update of multiple Font Asset
- Manual update is also possible

## Example

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226163642.gif)

By writing the character you want to include in FontAsset of TextMesh Pro in .txt and saving it.  
FontAsset is automatically updated to include only the characters listed in that .txt.  

Even if you are using Material Preset, it will update properly.  
It is also possible to automatically update multiple FontAsset at once.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164223.gif)

You can also disable automatic updating function and update manually.  

## How To Use

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164822.png)

Select "Create> TMP_Font Asset Updater Settings" in Project view.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164825.png)

A file for managing update rules of FontAsset is generated.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164828.png)

Set FontAsset to be updated, original font data, .txt describing the character to include in FontAsset in each item.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164831.png)

By pressing the "Update" button at the end, FontAsset is updated to include only the characters described in .txt.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226164834.png)

If "Is Auto Update" is checked, FontAsset will be automatically updated at the time of change in .txt.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226170142.png)

When updating of FontAsset is completed, the result is output to the Console window.  
You can check Missing Characters.  

![](https://cdn-ak.f.st-hatena.com/images/fotolife/b/baba_s/20181226/20181226165458.png)

Material Preset file name must be named using underbar.  
Material Preset that is not using underbar will not be eligible for updating.  

## Acknowledgments

This repository uses the code of the following repository.

- https://github.com/akof1314/UnityTMProFontCustomizedCreater

Example of this repository use the following free fonts.

- http://jikasei.me/font/rounded-mgenplus/
- http://www.fontna.com/blog/1706/
