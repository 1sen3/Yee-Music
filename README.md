
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
![1](https://github.com/user-attachments/assets/5727ffab-1503-4fa1-9fd8-b0fd0a99b5c1)
![2](https://github.com/user-attachments/assets/95e8f8f3-2dc6-46bf-b410-2dd47013c1fc)
![3](https://github.com/user-attachments/assets/f63a7642-3263-4450-b20d-2b9c66f35096)
![4](https://github.com/user-attachments/assets/a349c5f9-abbc-4963-865a-57f9e51facdc)
![5](https://github.com/user-attachments/assets/59ea9dac-3d08-4dd7-8f8e-0d58f9f87b95)
![6](https://github.com/user-attachments/assets/6e41ac72-085a-4bf7-afd7-3df5dcb946d8)
![7](https://github.com/user-attachments/assets/4e9e29cd-2133-4795-bc98-b00c223388fe)
![8](https://github.com/user-attachments/assets/46aa4ad6-7eaa-479e-950f-5a55a6ff2fe5)
### 开发者 Developer
1SenzZ2 - 2818634977@qq.com
### 鸣谢 Acknowledgements
- [Windows App SDK](https://github.com/microsoft/windowsappsdk)
- [WinUI](https://github.com/microsoft/microsoft-ui-xaml)
- [DevWinUI](https://github.com/ghost1372/DevWinUI)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
