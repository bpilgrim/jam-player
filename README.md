# jam-player

**A fast, tag-driven desktop media player for people with large local media libraries.**

Just Another Media Player. A Winamp-inspired Windows app for importing, organizing, and viewing your own images, GIFs, and video. Everything lives locally. Import from disk, tag it, then browse and filter by tag expressions instead of digging through folders. Built for speed, and for large personal collections that don't belong in the cloud.

> 🚧 **In active development, working toward commercial release.** This repository is the public home for the product: overview, roadmap, and architecture. The full application source is private.

<!-- Demo asset pending. See docs/img/. Drop in a short GIF of import → tag → filter → play and uncomment:
![jam-player browse view](docs/img/browse.png)
-->

---

## What it does

- **Import from disk.** Bring in images, GIFs, and video. Files are copied into a managed local store with generated thumbnails and extracted metadata.
- **Tag-based organization.** Organize assets with tags and tag groups instead of a folder hierarchy.
- **Filter by tag expressions.** Build a filter expression and the browse grid narrows to matching assets in real time.
- **Integrated viewers.** View images with pan/zoom, play GIFs, and play video, all on one player surface.
- **Local-first.** Your library and your data stay on your machine. No account, no cloud.

---

## Roadmap

**Now (toward MVP):** stable import → browse → tag-filter → view/play loop for images, GIFs, and video.

**Next:** dedicated import manager UI · fullscreen and alternate player layouts · asset & tag editor · video-frame thumbnail capture · a skin system (the Winamp-style promise).

---

## Under the hood

A modular Windows desktop application built with deliberate separation of concerns:

- **WPF on .NET 9**, composed with **Prism** (Unity) as independent feature modules: browser, navigation, tag picker, viewers, importer. They are wired through composite commands and an event aggregator rather than one monolithic controller.
- **EF Core over SQLite** behind a repository / unit-of-work layer (`IUnitOfWork` / `IRepository<T>`), so storage is swappable and testable.
- **Hardware-accelerated media rendering** with native tooling. `libmpv`/`ffmpeg` for video and thumbnails, `ExifTool` for metadata probing.
- **Background import pipeline.** A worker queue validates, shards, copies, and thumbnails imported files, publishing events the UI listens for.
- Unit and integration test projects across the asset browser, import, navigation, and tag-picker modules.

This repository is the product's public home. jam-player is a commercial product in development. The application source is not open source.

---

## Status

Pre-release. Core browse/import/view flows are implemented and building. Work is focused on finishing existing flows to a stable MVP rather than adding new surface area.

**Interested in early access or updates?** Reach me at bjp.business@gmail.com.
