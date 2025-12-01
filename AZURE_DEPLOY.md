# Hướng dẫn Deploy lên Azure

## 🐛 Fix lỗi JWT khi deploy

### Vấn đề vừa được fix:
- ❌ **Lỗi cũ**: `System.IdentityModel.Tokens.Jwt, Version=7.1.2.0` - Version không tồn tại
- ✅ **Đã fix**: Cập nhật lên version `8.2.1` (tương thích với .NET 8)
- ✅ **Bonus**: Cập nhật `Microsoft.AspNetCore.Authentication.JwtBearer` lên `8.0.11`

### Các bước sau khi fix:

1. **Restore packages**:
   ```bash
   dotnet restore
   ```

2. **Build lại project**:
   ```bash
   dotnet build --configuration Release
   ```

3. **Publish lại**:
   ```bash
   dotnet publish SEOBoostAI.API/SEOBoostAI.API.csproj -c Release -o ./publish
   ```

4. **Deploy lại lên Azure** (chọn 1 trong các cách sau):

---

## 📦 Cách 1: Deploy qua Azure Portal (Manual)

1. Build và publish locally:
   ```bash
   dotnet publish SEOBoostAI.API/SEOBoostAI.API.csproj -c Release -o ./publish
   ```

2. Nén thư mục `publish` thành file ZIP

3. Vào Azure Portal → App Service của bạn

4. Chọn **Deployment Center** → **ZIP Deploy**

5. Upload file ZIP và deploy

---

## 🚀 Cách 2: Deploy qua GitHub Actions (Recommended)

### Bước 1: Lấy Publish Profile từ Azure

1. Vào Azure Portal → App Service của bạn
2. Click **Get publish profile** (trên thanh toolbar)
3. Download file `.PublishSettings`
4. Mở file bằng text editor và copy toàn bộ nội dung

### Bước 2: Thêm Secret vào GitHub

1. Vào GitHub repository → **Settings**
2. Click **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Tạo secret:
   - Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Value: Paste nội dung publish profile vừa copy

### Bước 3: Thêm Secret tên App Service

1. Tạo thêm secret:
   - Name: `AZURE_WEBAPP_NAME`
   - Value: Tên App Service của bạn (ví dụ: `seoboostai-app`)

### Bước 4: Enable CD workflow

Uncomment phần Azure deploy trong `.github/workflows/cd.yml`:

```yaml
# === OPTION 1: Deploy to Azure App Service ===
- name: Deploy to Azure App Service
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ./publish
```

### Bước 5: Push code

```bash
git add .
git commit -m "Fix JWT version and enable Azure CD"
git push origin main
```

Workflow sẽ tự động deploy lên Azure! 🎉

---

## 🔧 Cách 3: Deploy qua Azure CLI

### Prerequisites
```bash
# Install Azure CLI
winget install Microsoft.AzureCLI

# Login
az login
```

### Deploy commands
```bash
# Build và publish
dotnet publish SEOBoostAI.API/SEOBoostAI.API.csproj -c Release -o ./publish

# Zip file
Compress-Archive -Path ./publish/* -DestinationPath ./publish.zip -Force

# Deploy
az webapp deployment source config-zip `
  --resource-group <your-resource-group> `
  --name <your-app-service-name> `
  --src ./publish.zip
```

---

## 🐳 Cách 4: Deploy bằng Docker Container

### Bước 1: Build Docker image
```bash
docker build -t seoboostai:latest .
```

### Bước 2: Tag image cho Azure Container Registry (ACR)
```bash
# Login to ACR
az acr login --name <your-acr-name>

# Tag image
docker tag seoboostai:latest <your-acr-name>.azurecr.io/seoboostai:latest

# Push to ACR
docker push <your-acr-name>.azurecr.io/seoboostai:latest
```

### Bước 3: Deploy to Azure App Service
```bash
az webapp config container set `
  --name <your-app-service-name> `
  --resource-group <your-resource-group> `
  --docker-custom-image-name <your-acr-name>.azurecr.io/seoboostai:latest `
  --docker-registry-server-url https://<your-acr-name>.azurecr.io
```

---

## ⚙️ Cấu hình Azure App Service

### 1. Cấu hình Connection Strings

Vào Azure Portal → App Service → **Configuration** → **Connection strings**:

```
Name: DefaultConnection
Value: <your-connection-string>
Type: SQLAzure (hoặc Custom)
```

### 2. Cấu hình App Settings

Thêm các environment variables cần thiết:

```
ASPNETCORE_ENVIRONMENT = Production
JWT_SECRET = <your-jwt-secret>
JWT_ISSUER = <your-issuer>
JWT_AUDIENCE = <your-audience>
GEMINI_API_KEY = <your-gemini-key>
```

### 3. Bật Application Insights (Optional)

Để monitoring và logging tốt hơn:

1. Vào App Service → **Application Insights**
2. Click **Turn on Application Insights**
3. Chọn existing hoặc tạo mới
4. Click **Apply**

---

## 🔍 Troubleshooting Azure Deployment

### Lỗi: Application Error
**Kiểm tra logs:**
```bash
# Via Azure CLI
az webapp log tail --name <your-app-name> --resource-group <your-rg>
```

Hoặc vào Azure Portal → App Service → **Log stream**

### Lỗi: 500 Internal Server Error

1. Bật detailed errors trong `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     }
   }
   ```

2. Check logs trong Azure Portal

### Lỗi: Database Connection Failed

1. Kiểm tra Connection String trong Configuration
2. Đảm bảo Azure SQL firewall cho phép Azure Services
3. Chạy migrations:
   ```bash
   dotnet ef database update --project SEOBoostAI.API
   ```

### Lỗi: JWT Still Not Working

Kiểm tra package versions đã được cập nhật:
```bash
dotnet list package
```

Phải thấy:
- `System.IdentityModel.Tokens.Jwt` → `8.2.1`
- `Microsoft.AspNetCore.Authentication.JwtBearer` → `8.0.11`

---

## 📊 Monitor Application

### Application Insights Queries

Vào Application Insights → **Logs**, chạy queries:

```kql
// Check errors
exceptions
| where timestamp > ago(1h)
| order by timestamp desc

// Check requests
requests
| where timestamp > ago(1h)
| summarize count() by resultCode
```

### Performance Monitoring

1. Vào App Service → **Metrics**
2. Thêm metrics:
   - Response Time
   - HTTP Server Errors
   - CPU Percentage
   - Memory Percentage

---

## 🎯 Best Practices

1. **Sử dụng Deployment Slots**:
   - Tạo `staging` slot để test trước khi swap lên production
   ```bash
   az webapp deployment slot create --name <app-name> --resource-group <rg> --slot staging
   ```

2. **Enable Auto-scaling**:
   - Vào Scale out (App Service plan)
   - Configure rules based on CPU/Memory

3. **Backup regularly**:
   - Vào Backups
   - Configure automated backups

4. **Use Azure Key Vault**:
   - Lưu secrets trong Key Vault thay vì App Settings
   - Tích hợp với Managed Identity

---

## 📝 Checklist Deploy

- [ ] Fix JWT package version → `8.2.1`
- [ ] Update JwtBearer package → `8.0.11`
- [ ] Test build locally
- [ ] Configure Connection Strings trong Azure
- [ ] Configure App Settings (JWT_SECRET, API keys, etc.)
- [ ] Deploy application
- [ ] Check logs trong Azure Portal
- [ ] Test API endpoints
- [ ] Setup Application Insights
- [ ] Configure auto-scaling
- [ ] Setup backup strategy

---

## 💡 Tips

1. **Local Testing giống Production**:
   ```bash
   # Set environment to Production
   $env:ASPNETCORE_ENVIRONMENT="Production"
   dotnet run
   ```

2. **Quick Redeploy**:
   - Sau khi đã setup GitHub Actions, chỉ cần `git push` là auto deploy

3. **Rollback nhanh**:
   - Vào Deployment Center → Deployment history
   - Click vào version muốn rollback → Redeploy

---

Nếu có thắc mắc hoặc gặp lỗi khác, check file [`TROUBLESHOOTING.md`](TROUBLESHOOTING.md)! 📚
