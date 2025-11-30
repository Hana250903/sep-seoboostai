# Hướng dẫn sử dụng CI/CD cho SEOBoostAI

## 📋 Tổng quan

Project này đã được cấu hình với GitHub Actions để tự động hóa quá trình build, test và deploy.

## 🔄 Các Workflow đã được tạo

### 1. **CI - Build and Test** (`ci.yml`)
- **Kích hoạt**: Khi push hoặc tạo PR vào nhánh `main` hoặc `develop`
- **Chức năng**:
  - Restore dependencies
  - Build solution
  - Chạy tests
  - Publish artifacts
  - Upload build artifacts (giữ 7 ngày)

### 2. **CD - Deploy to Production** (`cd.yml`)
- **Kích hoạt**: Khi push vào nhánh `main` hoặc trigger thủ công
- **Chức năng**: Deploy ứng dụng
- **Các options deploy**:
  - Azure App Service (đã comment)
  - Docker (đã comment)
  - SSH/SCP (đã comment)

### 3. **Docker Build & Push** (`docker-build.yml`)
- **Kích hoạt**: Khi push vào `main`/`develop`, hoặc tạo tag `v*`
- **Chức năng**:
  - Build Docker image
  - Push lên GitHub Container Registry (mặc định)
  - Hoặc Docker Hub (có thể enable)

## 🚀 Cách sử dụng

### Bước 1: Push code lên GitHub

```bash
git add .
git commit -m "Add CI/CD workflows"
git push origin main
```

### Bước 2: Cấu hình Secrets (nếu cần deploy)

Vào **Settings** → **Secrets and variables** → **Actions** và thêm các secrets cần thiết:

#### Để deploy lên Azure:
- `AZURE_WEBAPP_NAME`: Tên Azure Web App
- `AZURE_WEBAPP_PUBLISH_PROFILE`: Publish profile từ Azure

#### Để deploy với Docker Hub:
- `DOCKER_USERNAME`: Docker Hub username
- `DOCKER_PASSWORD`: Docker Hub password hoặc access token

#### Để deploy qua SSH:
- `SERVER_HOST`: IP hoặc domain của server
- `SERVER_USERNAME`: SSH username
- `SERVER_SSH_KEY`: SSH private key

### Bước 3: Enable workflow deploy

Mở file [cd.yml](.github/workflows/cd.yml) và uncomment phần deploy phù hợp với nhu cầu của bạn:

```yaml
# Ví dụ: Để deploy lên Azure, uncomment phần này:
- name: Deploy to Azure App Service
  uses: azure/webapps-deploy@v2
  with:
    app-name: ${{ secrets.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ./publish
```

### Bước 4: Tạo Environment (Optional nhưng recommended)

1. Vào **Settings** → **Environments**
2. Tạo environment mới tên `production`
3. Cấu hình protection rules nếu cần (ví dụ: yêu cầu approve trước khi deploy)

## 🐳 Sử dụng Docker

### Build local:
```bash
docker build -t seoboostai:latest .
```

### Run local:
```bash
docker run -p 8080:80 -p 8443:443 seoboostai:latest
```

### Pull từ GitHub Container Registry (sau khi workflow chạy):
```bash
docker pull ghcr.io/your-username/sep-seoboostai:main
```

## 📝 Customization

### Thay đổi .NET version
Sửa trong tất cả các workflow:
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'  # Thay đổi version ở đây
```

### Thêm test coverage
Thêm vào file `ci.yml`:
```yaml
- name: Test with coverage
  run: dotnet test --collect:"XPlat Code Coverage"

- name: Upload coverage to Codecov
  uses: codecov/codecov-action@v3
```

### Deploy lên nhiều environments
Tạo thêm các workflow files:
- `cd-staging.yml` cho staging environment
- `cd-production.yml` cho production environment

## 🔍 Kiểm tra workflow

1. Vào tab **Actions** trên GitHub repository
2. Chọn workflow muốn xem
3. Xem logs chi tiết của từng step

## ⚠️ Lưu ý quan trọng

1. **Bảo mật**: Không commit secrets trực tiếp vào code, luôn dùng GitHub Secrets
2. **Testing**: Đảm bảo có tests đầy đủ trước khi enable auto-deploy
3. **Branches**: Cân nhắc tạo nhánh `develop` cho development và `main` cho production
4. **Database migrations**: Thêm step chạy migrations trong CD workflow nếu cần

## 📚 Tài liệu tham khảo

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [.NET CI/CD Best Practices](https://docs.microsoft.com/en-us/dotnet/devops/)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## 🎯 Next Steps

- [ ] Push code lên GitHub
- [ ] Verify CI workflow chạy thành công
- [ ] Cấu hình secrets cho CD
- [ ] Uncomment và test CD workflow
- [ ] Setup monitoring và alerting
