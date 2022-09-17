#nullable disable

using System.ComponentModel.DataAnnotations;

namespace TodoList.Shared.Models
{
    public class SignupInfo
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "이메일을 입력해 주세요")]
        [EmailAddress(ErrorMessage = "잘못된 이메일 주소입니다")]
        public string Email { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "비밀번호를 입력해 주세요")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "비밀번호를 한번 더 입력해 주세요")]
        [Compare(nameof(Password), ErrorMessage = "비밀번호가 다릅니다")]
        [DataType(DataType.Password)]
        public string PasswordCheck { get; set; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "사용할 이름을 입력해 주세요")]
        public string Name { get; set; }
    }
}
