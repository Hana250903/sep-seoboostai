# Troubleshooting CI/CD

## ❌ Lỗi: "Could not load file or assembly 'System.IdentityModel.Tokens.Jwt'" (Azure Deploy)

### Nguyên nhân
Version number của package JWT không đúng format (sử dụng `7.1.2.0` thay vì `7.1.2`).

### ✅ Giải pháp đã áp dụng
Đã cập nhật packages trong `SEOBoostAI.API.csproj`:

```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
```

### Các bước sau khi fix:

1. **Restore và rebuild**:
   ```bash
   dotnet restore
   dotnet build --configuration Release
   ```

2. **Deploy lại lên Azure** (xem chi tiết trong [AZURE_DEPLOY.md](AZURE_DEPLOY.md))

---

## ❌ Lỗi: "installation not allowed to Create organization package"

### Nguyên nhân
GitHub Actions không có quyền tạo hoặc push packages lên GitHub Container Registry (GHCR).

### ✅ Giải pháp đã áp dụng
Đã thêm `permissions` vào workflow file:

```yaml
permissions:
  contents: read
  packages: write
```

### Các bước kiểm tra thêm

#### 1. Kiểm tra Package Settings (nếu vẫn bị lỗi)

Nếu package đã tồn tại, bạn cần cấp quyền cho GitHub Actions:

1. Vào repository trên GitHub
2. Click vào **Packages** (bên phải trang)
3. Click vào package name (ví dụ: `sep-seoboostai`)
4. Click **Package settings**
5. Scroll xuống **Manage Actions access**
6. Click **Add Repository** và thêm repository của bạn
7. Set role là **Write**

#### 2. Kiểm tra Repository Settings

Đảm bảo GitHub Actions được phép tạo packages:

1. Vào repository **Settings**
2. Click **Actions** → **General**
3. Scroll xuống **Workflow permissions**
4. Chọn **Read and write permissions**
5. Check ✅ **Allow GitHub Actions to create and approve pull requests**
6. Click **Save**

---

## ❌ Lỗi: Docker build failed

### Kiểm tra Dockerfile
Đảm bảo tất cả các project files tồn tại:

```bash
# Kiểm tra các files cần thiết
ls SEOBoostAI.API/SEOBoostAI.API.csproj
ls SEOBoostAI.Services/SEOBoostAI.Service.csproj
ls SEOBoostAI.Repositories/SEOBoostAI.Repository.csproj
```

### Build local để test
```bash
docker build -t test-build .
```

---

## ❌ Lỗi: CI workflow failed - Tests not found

### Giải pháp
Nếu chưa có test project, comment phần test trong `ci.yml`:

```yaml
# - name: Run tests
#   run: dotnet test SEP_SEOBoostAI.sln --configuration Release --no-build --verbosity normal
```

Hoặc tạo test project:
```bash
dotnet new xunit -n SEOBoostAI.Tests
dotnet sln add SEOBoostAI.Tests/SEOBoostAI.Tests.csproj
```

---

## 🐳 Alternative: Sử dụng Docker Hub thay vì GHCR

Nếu vẫn gặp vấn đề với GHCR, có thể dùng Docker Hub:

### 1. Tạo account Docker Hub
- Vào https://hub.docker.com
- Tạo tài khoản
- Tạo Access Token: Account Settings → Security → New Access Token

### 2. Thêm Secrets vào GitHub
- Vào repository **Settings** → **Secrets and variables** → **Actions**
- Click **New repository secret**
- Thêm:
  - `DOCKER_USERNAME`: username Docker Hub của bạn
  - `DOCKER_PASSWORD`: access token vừa tạo

### 3. Sửa workflow file

Sửa `.github/workflows/docker-build.yml`:

```yaml
    - name: Docker meta
      id: meta
      uses: docker/metadata-action@v5
      with:
        images: |
          your-dockerhub-username/seoboostai  # Thay your-dockerhub-username
        tags: |
          type=ref,event=branch
          type=semver,pattern={{version}}
          type=sha
          type=raw,value=latest,enable={{is_default_branch}}

    # Comment hoặc xóa GitHub Container Registry login
    # - name: Login to GitHub Container Registry
    #   uses: docker/login-action@v3
    #   with:
    #     registry: ghcr.io
    #     username: ${{ github.actor }}
    #     password: ${{ secrets.GITHUB_TOKEN }}

    # Uncomment Docker Hub login
    - name: Login to Docker Hub
      uses: docker/login-action@v3
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
```

---

## 📊 Kiểm tra workflow logs

1. Vào repository trên GitHub
2. Click tab **Actions**
3. Click vào workflow run bị lỗi
4. Click vào job để xem chi tiết logs
5. Mở rộng step bị lỗi để xem error message

---

## 🔍 Debug Commands

### Test Docker build locally
```bash
# Build only (không push)
docker build -t seoboostai:test .

# Run để test
docker run -p 8080:80 seoboostai:test

# Check logs
docker logs <container-id>
```

### Test .NET build locally
```bash
# Restore
dotnet restore SEP_SEOBoostAI.sln

# Build
dotnet build SEP_SEOBoostAI.sln --configuration Release

# Publish
dotnet publish SEOBoostAI.API/SEOBoostAI.API.csproj -c Release -o ./out
```

---

## 💡 Tips

1. **Luôn test local trước**: Build và test trên máy local trước khi push
2. **Kiểm tra logs**: Đọc kỹ error messages trong Actions logs
3. **Small commits**: Commit nhỏ để dễ debug khi có lỗi
4. **Use workflow_dispatch**: Enable manual trigger để test workflow

```yaml
on:
  workflow_dispatch:  # Cho phép chạy thủ công từ UI
  push:
    branches: [ main ]
```

---

## 📞 Hỗ trợ thêm

Nếu vẫn gặp vấn đề, cung cấp thông tin sau:
- ✅ Error message đầy đủ
- ✅ Link đến failed workflow run
- ✅ Logs của step bị lỗi
- ✅ Repository settings (có screenshot thì tốt)
