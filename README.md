
# Yee Music
基于 WinUI3 的本地音乐播放器
A local music player based on WinUI 3.
## 更新计划 Roadmap
- [x] 随机播放 Random Play
- [x] 完善播放列表 Improve Playlist
- [x] 完善播放栏 Improve Playback Bar
- [ ] 主页 Home Page
- [ ] 按歌手、专辑分类音乐 Categorize Music by Artist and Album
- [ ] 搜索 Search
- [ ] 快捷键 Keyboard Shortcuts
- [ ] 歌单 Playlists
- [ ] 歌词界面 Lyrics Interface
- [ ] 接入在线音乐平台 API Integrate Online Music Platform APIs
## 更新日志 Update Log
### 2025.3.7
1. 修复了列表循环模式下从长的歌曲切换到短的歌曲会跳过短的歌曲的问题
	Fixed the issue where switching from a longer song to a shorter song in list loop mode would skip the shorter song.
### 2025.3.6
1. 修复了通过播放栏切换歌曲时播放状态无法更新的问题
	Fixed the issue where the playback status would not update when switching songs via the playback bar.
2. 修复了在喜欢页面播放歌曲时播放列表不会更新的问题
	Fixed the issue where the playlist would not update when playing songs in the Favorites page.
3. 修复了列表循环模式下无法切换到列表中下一首歌曲的问题
	Fixed the issue where it was impossible to switch to the next song in the playlist in list loop mode.
4. 优化播放栏内歌曲名的显示效果
	Optimized the display effect of song names in the playback bar.
## 界面展示 Interface Showcase
![1](https://github.com/user-attachments/assets/e480b8c0-fb5b-44ac-9362-86d54df8ffd9)
![2](https://github.com/user-attachments/assets/f0b9333b-163f-40e0-83c7-2296ba3ee370)
![3](https://github.com/user-attachments/assets/f3d8b41c-6781-4409-aa59-414d092a6d64)
![4](https://github.com/user-attachments/assets/b81dfecd-8398-4477-bcaa-14fcc3c79b85)
![5](https://github.com/user-attachments/assets/638ddbb2-611f-4950-a4b5-956f486225cf)
![6](https://github.com/user-attachments/assets/84d919c3-41fc-4319-8add-93000a375cc0)
![7](https://github.com/user-attachments/assets/083b83e6-a95f-4a59-9968-a76a61e03344)
![8](https://github.com/user-attachments/assets/7c89f9a3-48c6-49d1-9b2e-83904d061cae)



### 开发者 Developer
1SenzZ2 - 2818634977@qq.com
### 鸣谢 Acknowledgements
- [Windows App SDK](https://github.com/microsoft/windowsappsdk)
- [WinUI](https://github.com/microsoft/microsoft-ui-xaml)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
