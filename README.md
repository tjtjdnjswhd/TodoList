# TodoList
Blazor, ASP.NET Core API로 제작한 할 일 목록 프로젝트입니다.
## Getting start
### Set TodoList.Server.appsettings.json
#### Connection string
 ```
"ConnectionStrings": {
  "Default": "<string>"
}
```
##### Mail
```
"MailSettings": {
  "From": "<string>",
  "DisplayName": "<string>",
  "Password": "<string>",
  "Host": "<string>",
  "Port": "<number>",
  "EnableSsl": "<boolean>"
},
```
##### Jwt
```
"JwtSettings": {
  "Issuer": "<string>",
  "Audience": "<string>",
  "SecretKey": "<string>",
  "SecurityAlgorithmName": "<string>"
},
```
##### Verify code
```
"VerifyCodeSettings": {
  "SlidingExpiration": "<string>"
},
```
##### Token setting
```
"AuthorizeTokenSetting": {
  "AccessTokenExpiration": "<TimeSpan>",
  "RefreshTokenExpiration": "<TimeSpan>",
  "AccessTokenKey": "<string>",
  "RefreshTokenKey": "<string>",
  "IsAccessTokenExpiredHeader": "IS-ACCESS-TOKEN-EXPIRED",
  "IsRefreshTokenExpiredHeader": "IS-REFRESH-TOKEN-EXPIRED"
},
```
##### Hash setting
```
"PasswordHashSettings": {
  "Pepper": "<string>",
  "SaltLength": "<number>",
  "HashLength": "<number>",
  "HashIterations": "<number>",
  "HashAlgorithmName": "<string>"
}
```
### Data migration
```
update-database -project TodoList.Migrations
```
## Reference
+ [AutoMapper](https://automapper.org/)
+ [MailKit](https://github.com/jstedfast/MailKit)
+ [NLog](https://nlog-project.org/)
+ [Newtonsoft.Json](https://www.newtonsoft.com/json)
## Authorize
cookie + jwt + refresh token
