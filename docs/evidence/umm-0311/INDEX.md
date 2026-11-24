# UMM 0.31.1 baseline evidence

Machine-generated outputs for candidate commit `e3be08363f6910e1890e776d5b193a7a98154e51`, exported 2026-11-24 00:52 UTC.
Only local path prefixes were replaced: `<repo>` (this checkout or its temporary clone root), `<temp>`, `<game>`
(the Kingmaker installation, read-only), `<buildtools>`, `<reference-assemblies>`, `<dotnet>`, `<user-profile>`, `<fleet>` (the owner's fleet mission folder,
outside this repository). Line endings were normalized to LF and a UTF-8 byte-order mark was dropped; nothing else was
edited. Raw SHA-256 is of the original output bytes. Exported SHA-256 is of the committed Git blob of each file in this
folder (LF bytes; a Windows checkout with `core.autocrlf` may show CRLF in the working tree, so hash the blob, not the
checked-out file). To verify one, from the repository root:

```
git cat-file blob HEAD:docs/evidence/umm-0311/<file> | sha256sum
python -c "import hashlib,subprocess,sys; print(hashlib.sha256(subprocess.check_output(['git','cat-file','blob','HEAD:docs/evidence/umm-0311/'+sys.argv[1]])).hexdigest())" <file>
```

| File | Raw SHA-256 | Exported SHA-256 (Git blob) |
| --- | --- | --- |
| `build-record.json` | `edaabc1525342b6188364fc5d2cdb656698176c02ed61ddcfdbd0ffe3b5de5fe` | `fa75d75434694bc4636967fb9862c2a67128b7a1c86cb876dd1f454079794493` |
| `build-provenance.json` | `ed81f6010100b11e17fec28845beee695b92b5bee43a7d768ab13ed75ffb8aef` | `7e3ea2acc38714d2c15da7ad45a467b8a87408d0fff1dbdc9e1c73ea616d9a25` |
| `package-manifest.json` | `ca04b47f1654cf96bf1155202c64362083183147c1742844c4bab79d93b40902` | `79a5322abc24a170f3c8fc645a8f131d6422ca29d39257ad1d3a07f8450c23ea` |
| `package-validation.json` | `e5ac288a2639173c474e544f979b371cdcdf70d86f67ade74eb9f58da6f80784` | `74eb6c3dab18fb193f0c81f657b841ea5153f28bf4533e944135e79f36df4e1c` |
| `loader-baseline.json` | `27347fd3082774187b103cf92c84fd0576e72fac0a123f782afd758274c7260c` | `b21f7191bc9310640c805109cdde159f168cf75f4fc84573e53799a370b12eec` |
| `api-audit.json` | `c38d49d4eb66f8bfdaeded0c8d97ccdaaf4e31cf8466e170de3ba52a33780019` | `9bd77723164ffb889889b7358186e33d2ec9e8f78ee686fad886e6cdb6709932` |
| `loader-profile-inventory.json` | `ae9abd61d8aad4e00a41e08ff98eb60b2dda29348f53f01ea29e0e06db1381d8` | `40b637a0a98dfa57928c80bc12d02b7c8fc2767b94df5daa21c4e63d58f4dc30` |
| `docs-link-check.json` | `7cae4db30deb6e287f49508c9cd08ab9bcc567c87546d88061747d90fb3ca666` | `8422cdcd02e1069b31ba5c28979ab9ca2b83a7216f3bdd58e09431e25ee69584` |
| `newest-stable-selection.json` | `3b3afcb904f00b33006d74c481fbce4bf6e4992e7f9001d28cdf28d589f9f56f` | `150806fc23701c046ae9980c73c91d12c3badb671d7e30c126b61060784dd51e` |
| `loader-profiles.md` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` | `3b2883677f537286a0d6f210327fae8ac8734b40ec7cc5850e92819eaeb03d5e` |
| `loader-api-audit-Program.cs.txt` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` | `be870827024fd41f63235ebb522c489e8d49c7991de7091ad0e14a1a0e935ea3` |
| `loader-api-audit-csproj.txt` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` | `93989652d8252ff54a60ec92c13c1fc41bf94bfa3cf6c9f7f13c877bac761909` |
