### Build and Run
*   **Build the project**: `dotnet build`
*   **Run the project (from source)**: `dotnet run`
*   **Publish the project (self-contained executable)**: `dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true`
    *   **Note on .exe**: Trimming (`/p:PublishTrimmed=true`) is not supported for WPF applications and will cause build errors. The resulting executable will be larger due to bundling the .NET runtime.
    *   **Note on .exe execution**: If the published `.exe` does not run, it might be due to missing Visual C++ Redistributable packages. Running from the command line (`KefuTimer.exe > output.txt 2>&1`) can help diagnose issues.

### Testing
*   **Run tests**: `dotnet test` (Note: No test files are currently present, so tests would need to be added first.)

### Utility Commands (Windows)
*   **List directory contents**: `dir`
*   **Change directory**: `cd <directory_name>`
*   **Search for files/content**: `findstr` (for content), `dir /s` (for files)
*   **Version control**: `git` commands (e.g., `git status`, `git add`, `git commit`)

### After making changes, remember to:
1.  **Build the project**: `dotnet build`
2.  **Run the application**: `dotnet run` to verify functionality.