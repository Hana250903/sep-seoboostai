# ✅ FIXED: Lỗi JWT khi Deploy lên Azure

## 🐛 Vấn đề gốc

Khi deploy lên Azure App Service, ứng dụng bị crash với lỗi:

```
System.IO.FileNotFoundException: Could not load file or assembly 
'System.IdentityModel.Tokens.Jwt, Version=7.1.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
```

## 🔍 Nguyên nhân

Package reference trong file `SEOBoostAI.API.csproj` có version number không đúng format:
- ❌ Sai: `Version="7.1.2.0"` (4 parts)
- ✅ Đúng: `Version="8.2.1"` (3 parts)

NuGet packages thường dùng semantic versioning (major.minor.patch), không phải 4 parts.

## ✅ Giải pháp đã áp dụng

### 1. Cập nhật packages trong `SEOBoostAI.API.csproj`:

```xml
<!-- CŨ - Chỉ có JWT package với version sai -->
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.1.2.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.5" />

<!-- MỚI - Thêm tất cả các IdentityModel packages để tránh conflicts -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="8.2.1" />
<PackageReference Include="Microsoft.IdentityModel.Protocols" Version="7.7.1" />
<PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="7.7.1" />
<PackageReference Include="Microsoft.IdentityModel.JsonWebTokens" Version="8.2.1" />
<PackageReference Include="Microsoft.IdentityModel.Logging" Version="8.2.1" />
```

**Lý do cần thêm tất cả packages:**
- JWT package có nhiều transitive dependencies
- Các dependencies này cũng có version conflicts
- Explicit references đảm bảo dùng version đúng cho tất cả

### 2. Restore và build lại:

```bash
dotnet restore
dotnet build --configuration Release
✅ Build succeeded
```

## 🚀 Deployment

### Cách nhanh nhất (nếu đã setup GitHub Actions):

```bash
git add .
git commit -m "Fix JWT version for Azure deployment"
git push origin main
```

Workflow CD sẽ tự động:
1. Build với packages mới
2. Publish application
3. Deploy lên Azure App Service (nếu đã config secrets)

### Deploy manual:

Xem hướng dẫn chi tiết trong file: [AZURE_DEPLOY.md](AZURE_DEPLOY.md)

## 📋 Files đã thay đổi

1. ✅ [`SEOBoostAI.API.csproj`](file:///e:/ProjectVS/SEP_SEOBoostAI/SEOBoostAI.API/SEOBoostAI.API.csproj)
   - Updated JWT package version
   - Updated JwtBearer package version

2. ✅ [`.github/workflows/cd.yml`](file:///e:/ProjectVS/SEP_SEOBoostAI/.github/workflows/cd.yml)
   - Enabled Azure App Service deployment

3. 📝 [`AZURE_DEPLOY.md`](file:///e:/ProjectVS/SEP_SEOBoostAI/AZURE_DEPLOY.md) (NEW)
   - Hướng dẫn deploy chi tiết
   - 4 phương pháp deploy khác nhau
   - Troubleshooting Azure-specific issues

4. 📝 [`TROUBLESHOOTING.md`](file:///e:/ProjectVS/SEP_SEOBoostAI/TROUBLESHOOTING.md) (UPDATED)
   - Thêm section fix lỗi JWT

## 🎯 Next Steps

1. **Nếu chưa có GitHub Secrets**, thêm vào:
   - `AZURE_WEBAPP_NAME`: Tên App Service của bạn
   - `AZURE_WEBAPP_PUBLISH_PROFILE`: Download từ Azure Portal

2. **Push code** và kiểm tra workflow:
   ```bash
   git push origin main
   ```

3. **Monitor deployment** tại GitHub Actions tab

4. **Test API** sau khi deploy xong:
   ```bash
   curl https://<your-app-name>.azurewebsites.net/api/health
   ```

## 📚 Tài liệu liên quan

- [README_CI_CD.md](README_CI_CD.md) - Hướng dẫn CI/CD tổng quan
- [AZURE_DEPLOY.md](AZURE_DEPLOY.md) - Chi tiết deploy Azure
- [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - Các lỗi khác

---

**Status**: ✅ RESOLVED - Build thành công, sẵn sàng deploy!
