using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Typsa.Sso;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using TYPSA.SharedLib.Json;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class cls_00_ButtonAteneaRegister
    {
        // -----------------------------
        // Authenticate
        // -----------------------------

        public static async Task<AteneaLoginResult> AuthenticateInAteneaAsync(
            string email
        )
        {
            // -----------------------------
            // Configurar SSO
            // -----------------------------

            cls_00_TypsaSsoClient client = cls_00_TypsaSsoClient.SetConfig();

            // -----------------------------
            // Login Cognito / TYPSA SSO
            // -----------------------------

            TokenResponse tokens = await client.AuthenticateStrictAsync(email);

            // Validamos
            if (string.IsNullOrWhiteSpace(tokens.IdToken))
            {
                throw new SsoAuthException(
                    "Cognito id_token was not returned."
                );
            }

            if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                throw new SsoAuthException(
                    "Cognito refresh_token was not returned."
                );
            }

            // -----------------------------
            // Intercambiar tokens con ATENEA
            // -----------------------------

            using (HttpClient http = new HttpClient())
            {
                string requestBody = JsonConvert.SerializeObject(
                    new
                    {
                        mail = email,
                        username = email
                    }
                );

                using (
                    HttpRequestMessage request = new HttpRequestMessage(
                        HttpMethod.Post, cls_00_AteneaSsoConfig.AteneaSsoUrl
                    )
                )
                {
                    request.Content = new StringContent(
                        requestBody, Encoding.UTF8, "application/json"
                    );

                    request.Headers.TryAddWithoutValidation(
                        "x-cognito-id-token",
                        tokens.IdToken
                    );

                    request.Headers.TryAddWithoutValidation(
                        "x-cognito-refresh-token",
                        tokens.RefreshToken
                    );

                    // -----------------------------
                    // Enviar
                    // -----------------------------

                    using (HttpResponseMessage response = await http.SendAsync(request))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        // Validamos
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException(
                                $"ATENEA SSO returned " +
                                $"{(int)response.StatusCode} " +
                                $"{response.StatusCode}.\n\n" +
                                responseBody
                            );
                        }

                        // -----------------------------
                        // Obtener Access Token ATENEA
                        // -----------------------------

                        string accessToken = cls_00_SaveJson.ReadJsonString(
                            responseBody,
                            "accessToken"
                        );
                        // Validamos
                        if (string.IsNullOrWhiteSpace(accessToken))
                        {
                            throw new SsoAuthException(
                                "ATENEA SSO response did not include accessToken."
                            );
                        }

                        // -----------------------------
                        // Return
                        // -----------------------------

                        return new AteneaLoginResult
                        {
                            Email = email,
                            AccessToken = accessToken,
                            CognitoIdToken = tokens.IdToken,
                            CognitoRefreshToken = tokens.RefreshToken
                        };
                    }
                }
            }
        }

        // -----------------------------
        // ATENEA Register
        // -----------------------------

        public static void ButtonAteneaRegister()
        {
            // try
            try
            {
                // -----------------------------
                // Obtener idioma
                // -----------------------------

                bool isSpanish = IsCivilSpanish();

                // -----------------------------
                // Obtener usuario
                // -----------------------------

                string email = cls_00_AteneaUser.GetUserEmail(isSpanish);
                // Validamos
                if (string.IsNullOrWhiteSpace(email)) return;

                // -----------------------------
                // Autenticar usuario
                // -----------------------------

                AteneaLoginResult loginResult = Task.Run(
                    () => AuthenticateInAteneaAsync(email)
                ).GetAwaiter().GetResult();

                // Validamos
                if (
                    loginResult == null ||
                    string.IsNullOrWhiteSpace(loginResult.AccessToken)
                )
                {
                    MessageBox.Show(
                        isSpanish
                            ? "No se pudo completar la autenticación."
                            : "Authentication could not be completed.",
                        "ATENEA",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning
                    );
                    return;
                }

                // -----------------------------
                // Guardar sesión ATENEA
                // -----------------------------

                cls_00_AteneaSession.Email = loginResult.Email;
                cls_00_AteneaSession.AccessToken = loginResult.AccessToken;

                // -----------------------------
                // Habilitar herramientas ATENEA
                // -----------------------------

                AppAteneaSSO.SetAteneaToolsEnabled(true);

                // -----------------------------
                // Usuario registrado
                // -----------------------------

                MessageBox.Show(
                    isSpanish
                        ? $"Autenticación completada correctamente.\n\nUsuario: {email}"
                        : $"Authentication successful.\n\nUser: {email}",
                    "ATENEA",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            // catch
            catch (SsoAuthException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ATENEA Authentication Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ATENEA Connection Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "ATENEA Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }
        }


    }
}