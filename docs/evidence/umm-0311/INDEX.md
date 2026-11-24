# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `dd5483862e95fe10a4c307d0eaa8e70c9467f57d`, exported 2026-11-24 01:52 UTC.
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

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `build-record.json` | `bde6579c870193c51a8c9c9dd3ce945edb4907b611cd8aebf18a758edc6b597f` | `b3795cfde8c0114b411623bfe9d686e19dc378cdf64be80d88c4a04531d10ae0` |
| `build-provenance.json` | `abafdcb96df4198cf3f2bdd6b9381ea4ed1a11c97147906a53744e5971689992` | `f1e2551ce21d46ef28f098cf9882da73dfd177c2715855b04c61cef3d4394e43` |
| `package-manifest.json` | `075612193b8f291c8b83c77e0216611dfd9aaa18c8e071585fc72f6196596cf8` | `08598058d1afd01c1949f73c5b32be0a6193cdc10f995cea75b00d2a01aa0a21` |
| `package-validation.json` | `1f159baee369e47afe6299082f4033267c56136798b5ee23c83edc16dbd60557` | `cb709bccde945c52c62db4a5cd3ae1f631c3b877de8f77a0f416db1def56108c` |
| `loader-baseline.json` | `f5ef3d89b81ad15131fa8d069fd8f9bb70c9494fdf29d798cd05cfa1ebcf52bf` | `d53d10471524d913fdac3a842026a5d65845bcece6f8b65fe85d272aa1875260` |
| `api-audit.json` | `c38d49d4eb66f8bfdaeded0c8d97ccdaaf4e31cf8466e170de3ba52a33780019` | `9bd77723164ffb889889b7358186e33d2ec9e8f78ee686fad886e6cdb6709932` |
| `loader-profile-inventory.json` | `f361c14a69acd3985005f6f49116c29278dd138dcd41101e6be882e7f2e6c868` | `95ee8f30aa144ab343cca54f508ae22b0d92eeb90bf6e38b5ae92e60ffcf7a24` |
| `docs-link-check.json` | `7cae4db30deb6e287f49508c9cd08ab9bcc567c87546d88061747d90fb3ca666` | `8422cdcd02e1069b31ba5c28979ab9ca2b83a7216f3bdd58e09431e25ee69584` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
