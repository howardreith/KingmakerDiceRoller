# UMM 0.31.1 baseline evidence

Machine-generated outputs exported 2026-11-25 10:31 UTC for candidate commit `e89e49ef1b5635a9552692c7912b36ff5db839f2`. Each row names what produced that output (its
source commit, or the reused input it describes); outputs produced at another commit are identified there.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<controller>` (the agent-review controller), `<dev>` (the owner's development folder), `<steam>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root (Git Bash or any POSIX shell with sha256sum; the second
command, with Python, works in Windows PowerShell and cmd.exe):

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Produced by | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- | --- |
| `build-provenance.json` | 8c198e0 (Build-Local.ps1, clean tree; tooling f35f6a7 + test ed8eb3b; product unchanged since) | `79971e038a5531f4df320f37b68ee2baedad8039a6f0abd40301d070c91c812e` (original output, from its first export) | `d2bb425e36591a6b513e29dbd2a7c83c46041a38cc8718d18885f21a38cbe2e8` |
| `build-local-summary.json` | 8c198e0 (result lines of that Build-Local.ps1 and Package.ps1 run) | `0953445663e1994d349b23f665dd63af72adae4a69749df1a34b0e8971af59c1` (original output, from its first export) | `6860fdfba70749dbf87a975e8b931a6b4ca8b2ca0f31b1ff1bd393cb24a5945d` |
| `package-manifest.json` | 8c198e0 (Package.ps1 on that build) | `8d01f30711baaa0e28271068c85d2971c4f8186955556a4840e30f96dba2f4ec` (original output, from its first export) | `e210bcb2cdd0f8b8899a91f5ea54fd396e33600c6443577c4cc19f2e12b726eb` |
| `package-validation.json` | 8c198e0 (Package.ps1 on that build) | `d8d56551bc3fde2173dba6363080a8dc3c4432f1f625ff9f5f6ee2e3da1ff23d` (original output, from its first export) | `d6bd9c6b30ec71988677dc49a7466bbebe15449443bb9ae5aaed605c7313c4f5` |
| `loader-baseline.json` | e89e49e (temporary clone; 147/147; tooling 4effc85 + e89e49e) | `5fcf92e4c221be6a6dffa3cd768e16acf791349480f2c869620d4c07a91ee701` | `8be7dc4c24619fc43e21a46939a7d7efc61161a6bccdeec5e54efa60b9bccb55` |
| `docs-link-check.json` | `8c198e0` | `64b18869dfa5adf15f474c7653288cbe5a513869b0f7805deaf314703a843848` (original output, from its first export) | `4846d9427e30426d3a208698e72154d2d77688d6567670604f8afbf0cff11487` |
| `api-audit.json` | reused static audit of the unchanged candidate DLL 634f2277 | `c38d49d4eb66f8bfdaeded0c8d97ccdaaf4e31cf8466e170de3ba52a33780019` (original output, from its first export) | `9bd77723164ffb889889b7358186e33d2ec9e8f78ee686fad886e6cdb6709932` |
| `loader-profile-inventory.json` | reused profile inventory with the unchanged DLL 634f2277 | `f361c14a69acd3985005f6f49116c29278dd138dcd41101e6be882e7f2e6c868` (original output, from its first export) | `95ee8f30aa144ab343cca54f508ae22b0d92eeb90bf6e38b5ae92e60ffcf7a24` |
| `newest-stable-selection.json` | reused fleet record measured 2026-11-23 | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` (original output, from its first export) | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | fleet loader-profile record (reused) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` (original output, from its first export) | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | audit tool source (reused) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` (original output, from its first export) | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | audit tool project (reused) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` (original output, from its first export) | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
