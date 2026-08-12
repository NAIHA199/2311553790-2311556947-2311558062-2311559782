namespace LibraryAdvanced.Services
{
    public static class PasswordHasher
    {
        // Mã hóa mật khẩu thô thành chuỗi băm an toàn
        public static string HashPassword(string rawPassword)
        {
            return BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 12);
        }

        // Xác thực mật khẩu khi đăng nhập
        public static bool VerifyPassword(string rawPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(rawPassword, hashedPassword);
        }
    }
}