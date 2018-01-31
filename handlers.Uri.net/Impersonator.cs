using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;


public class Impersonator : IDisposable
{
    #region Public methods.
    // ------------------------------------------------------------------

    /// <summary>
    /// Constructor. Starts the impersonation with the given credentials.
    /// Please note that the account that instantiates the Impersonator class
    /// needs to have the 'Act as part of operating system' privilege set.
    /// </summary>
    /// <param name="userName">The name of the user to act as.</param>
    /// <param name="domainName">The domain name of the user to act as.</param>
    /// <param name="password">The password of the user to act as.</param>
    public Impersonator(
        string userName,
        string domainName,
        string password)
    {
        ImpersonateValidUser( userName, domainName, password );
    }

    // ------------------------------------------------------------------
    #endregion

    #region IDisposable member.
    // ------------------------------------------------------------------

    public void Dispose()
    {
        UndoImpersonation();
    }

    // ------------------------------------------------------------------
    #endregion

    #region P/Invoke.
    // ------------------------------------------------------------------

    [DllImport( "advapi32.dll", SetLastError = true )]
    private static extern int LogonUser(
        string lpszUserName,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        ref IntPtr phToken);

    [DllImport( "advapi32.dll", CharSet = CharSet.Auto, SetLastError = true )]
    private static extern int DuplicateToken(
        IntPtr hToken,
        int impersonationLevel,
        ref IntPtr hNewToken);

    [DllImport( "advapi32.dll", CharSet = CharSet.Auto, SetLastError = true )]
    private static extern bool RevertToSelf();

    [DllImport( "kernel32.dll", CharSet = CharSet.Auto )]
    private static extern bool CloseHandle(
        IntPtr handle);

    private const int LOGON32_LOGON_INTERACTIVE = 2;
    private const int LOGON32_LOGON_NETWORK = 3;
    private const int LOGON32_LOGON_BATCH = 4;
    private const int LOGON32_LOGON_SERVICE = 5;
    private const int LOGON32_LOGON_UNLOCK = 7;
    private const int LOGON32_LOGON_NETWORK_CLEARTEXT = 8; // Win2K or higher
    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9; // Win2K or higher

    private const int LOGON32_PROVIDER_DEFAULT = 0;
    private const int LOGON32_PROVIDER_WINNT35 = 1;
    private const int LOGON32_PROVIDER_WINNT40 = 2;
    private const int LOGON32_PROVIDER_WINNT50 = 3;
    // ------------------------------------------------------------------
    #endregion

    #region Private member.
    // ------------------------------------------------------------------

    /// <summary>
    /// Does the actual impersonation.
    /// </summary>
    /// <param name="userName">The name of the user to act as.</param>
    /// <param name="domainName">The domain name of the user to act as.</param>
    /// <param name="password">The password of the user to act as.</param>
    private void ImpersonateValidUser(
        string userName,
        string domain,
        string password)
    {
        WindowsIdentity tempWindowsIdentity = null;
        IntPtr token = IntPtr.Zero;
        IntPtr tokenDuplicate = IntPtr.Zero;

        try
        {
            if ( RevertToSelf() )
            {
                if ( LogonUser(
                    userName,
                    domain,
                    password,
                    LOGON32_LOGON_NETWORK,
                    LOGON32_PROVIDER_DEFAULT,
                    ref token ) != 0 )
                {
                    if ( DuplicateToken( token, 2, ref tokenDuplicate ) != 0 )
                    {
                        tempWindowsIdentity = new WindowsIdentity( tokenDuplicate );
                        impersonationContext = tempWindowsIdentity.Impersonate();
                    }
                    else
                    {
                        throw new Win32Exception( Marshal.GetLastWin32Error() );
                    }
                }
                else
                {
                    throw new Win32Exception( Marshal.GetLastWin32Error() );
                }
            }
            else
            {
                throw new Win32Exception( Marshal.GetLastWin32Error() );
            }
        }
        finally
        {
            if ( token != IntPtr.Zero )
            {
                CloseHandle( token );
            }
            if ( tokenDuplicate != IntPtr.Zero )
            {
                CloseHandle( tokenDuplicate );
            }
        }
    }

    /// <summary>
    /// Reverts the impersonation.
    /// </summary>
    private void UndoImpersonation()
    {
        if ( impersonationContext != null )
        {
            impersonationContext.Undo();
        }
    }

    private WindowsImpersonationContext impersonationContext = null;

    // ------------------------------------------------------------------
    #endregion
}

public enum LogonType
{
    LOGON32_LOGON_INTERACTIVE = 2,
    LOGON32_LOGON_NETWORK = 3,
    LOGON32_LOGON_BATCH = 4,
    LOGON32_LOGON_SERVICE = 5,
    LOGON32_LOGON_UNLOCK = 7,
    LOGON32_LOGON_NETWORK_CLEARTEXT = 8, // Win2K or higher
    LOGON32_LOGON_NEW_CREDENTIALS = 9 // Win2K or higher
};

public enum LogonProvider
{
    LOGON32_PROVIDER_DEFAULT = 0,
    LOGON32_PROVIDER_WINNT35 = 1,
    LOGON32_PROVIDER_WINNT40 = 2,
    LOGON32_PROVIDER_WINNT50 = 3
};

public enum ImpersonationLevel
{
    SecurityAnonymous = 0,
    SecurityIdentification = 1,
    SecurityImpersonation = 2,
    SecurityDelegation = 3
}
