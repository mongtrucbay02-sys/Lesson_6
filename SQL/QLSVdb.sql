

-- 1. Tạo database
IF DB_ID(N'QuanLySinhVienDB') IS NULL
    CREATE DATABASE QuanLySinhVienDB;
GO

USE QuanLySinhVienDB;
GO

-- 2. Xóa bảng cũ nếu có (để chạy lại nhiều lần)
IF OBJECT_ID(N'dbo.SinhViens', N'U') IS NOT NULL DROP TABLE dbo.SinhViens;
IF OBJECT_ID(N'dbo.Khoas', N'U') IS NOT NULL DROP TABLE dbo.Khoas;
GO

-- 3. Bảng Khoas
CREATE TABLE dbo.Khoas (
    Id      INT IDENTITY(1,1) NOT NULL,
    TenKhoa NVARCHAR(100)     NOT NULL,
    CONSTRAINT PK_Khoas PRIMARY KEY (Id)
);
GO

-- 4. Bảng SinhViens
CREATE TABLE dbo.SinhViens (
    Id          INT IDENTITY(1,1) NOT NULL,
    MaSV        NVARCHAR(20)      NOT NULL,
    HoTen       NVARCHAR(100)     NOT NULL,
    NgaySinh    DATETIME2         NOT NULL,
    GioiTinh    NVARCHAR(10)      NOT NULL,
    Email       NVARCHAR(100)     NULL,
    SoDienThoai NVARCHAR(15)      NULL,
    DiaChi      NVARCHAR(200)     NULL,
    KhoaId      INT               NOT NULL,
    CONSTRAINT PK_SinhViens PRIMARY KEY (Id),
    CONSTRAINT FK_SinhViens_Khoas FOREIGN KEY (KhoaId)
        REFERENCES dbo.Khoas (Id) ON DELETE NO ACTION
);
GO

-- Mã sinh viên không được trùng
CREATE UNIQUE INDEX IX_SinhViens_MaSV ON dbo.SinhViens (MaSV);
CREATE INDEX IX_SinhViens_KhoaId ON dbo.SinhViens (KhoaId);
GO

-- 5. Dữ liệu mẫu
SET IDENTITY_INSERT dbo.Khoas ON;
INSERT INTO dbo.Khoas (Id, TenKhoa) VALUES
    (1, N'Công nghệ thông tin'),
    (2, N'Kinh tế'),
    (3, N'Ngoại ngữ'),
    (4, N'Điện - Điện tử');
SET IDENTITY_INSERT dbo.Khoas OFF;

SET IDENTITY_INSERT dbo.SinhViens ON;
INSERT INTO dbo.SinhViens (Id, MaSV, HoTen, NgaySinh, GioiTinh, Email, SoDienThoai, DiaChi, KhoaId) VALUES
    (1, N'SV001', N'Nguyễn Văn An',  '2004-03-15', N'Nam', N'an.nv@example.com',   N'0901234567', N'Hà Nội',    1),
    (2, N'SV002', N'Trần Thị Bình',  '2004-07-21', N'Nữ',  N'binh.tt@example.com', N'0912345678', N'Hải Phòng', 2);
SET IDENTITY_INSERT dbo.SinhViens OFF;
GO

-- 6. Kiểm tra
SELECT * FROM dbo.Khoas;
SELECT sv.Id, sv.MaSV, sv.HoTen, sv.NgaySinh, sv.GioiTinh, k.TenKhoa
FROM dbo.SinhViens sv
JOIN dbo.Khoas k ON k.Id = sv.KhoaId;
GO