# Civil 3D Point Style Modifier

A C# .NET plugin for Autodesk Civil 3D that allows users to easily modify COGO point styles through an intuitive dialog interface.

## Features

- Select multiple COGO points in your drawing
- Choose from all available point styles via a dialog box
- Apply the selected style to all selected points with one click
- Built-in error handling and user feedback

## Compatibility

- Civil 3D 2020, 2021, 2022, and 2023
- .NET Framework 4.8
- Windows x64

## Installation

### Prerequisites

- Visual Studio 2017 or later (for building from source)
- Autodesk Civil 3D 2020-2023 installed

### Building the Project

1. Open `PointStyleModifier.sln` in Visual Studio
2. Update the DLL reference paths in `PointStyleModifier.csproj` to match your Civil 3D installation:
   - Right-click on the project and select "Edit Project File"
   - Update the `HintPath` for each reference to point to your Civil 3D installation directory
   - Typical path: `C:\Program Files\Autodesk\AutoCAD 2023\`
3. Build the solution in Release mode (Build > Build Solution)
4. The compiled DLL will be in `PointStyleModifier\bin\Release\PointStyleModifier.dll`

### Loading the Plugin

1. Open Civil 3D
2. Type `NETLOAD` at the command line and press Enter
3. Browse to and select `PointStyleModifier.dll`
4. The plugin is now loaded and ready to use

## Usage

### Step-by-Step Instructions

1. **Load the plugin** (if not already loaded):
   - Type `NETLOAD` and select the DLL file

2. **Run the command**:
   - Type `MODIFYPOINTSTYLE` at the command line and press Enter

3. **Select COGO points**:
   - The command will prompt: "Select COGO points:"
   - Select the points you want to modify (individual picks, window selection, etc.)
   - Press Enter when done selecting

4. **Choose a point style**:
   - A dialog box will appear showing all available point styles in the current drawing
   - Select the desired style from the list
   - Double-click or click OK to apply

5. **Confirmation**:
   - A success message will display: "Successfully applied style 'StyleName' to X point(s)"

### Command Reference

| Command | Description |
|---------|-------------|
| `MODIFYPOINTSTYLE` | Opens the point style modifier tool |

### Tips

- Only COGO points will be selected (other objects are automatically filtered)
- You can select points before running the command (implied selection)
- Double-clicking a style in the dialog applies it immediately
- Press Escape or click Cancel to abort the operation

## Project Structure

```
point-style-modifier/
├── PointStyleModifier.sln              # Visual Studio solution
├── PointStyleModifier/
│   ├── Commands/
│   │   └── PointStyleCommand.cs        # Main AutoCAD command
│   ├── Services/
│   │   ├── PointStyleService.cs        # Point style business logic
│   │   └── PointSelectionService.cs    # Selection handling
│   ├── UI/
│   │   ├── PointStyleSelectorForm.cs   # Windows Forms dialog
│   │   └── PointStyleSelectorForm.Designer.cs
│   ├── Models/
│   │   └── PointStyleInfo.cs           # Data model
│   ├── Utilities/
│   │   └── ErrorHandler.cs             # Error handling
│   └── Properties/
│       └── AssemblyInfo.cs             # Assembly information
└── README.md                            # This file
```

## Error Handling

The plugin handles common error scenarios:

- **No points selected**: Displays message "No COGO points were selected"
- **Selection cancelled**: Silently exits the command
- **No point styles available**: Warns "No point styles found in the current drawing"
- **Dialog cancelled**: Displays "Command cancelled"
- **Transaction errors**: Shows detailed error message

## Troubleshooting

### Issue: "Could not load file or assembly" error

**Solution**: Ensure all DLL references in the .csproj file point to your actual Civil 3D installation directory.

### Issue: Command not found after NETLOAD

**Solution**:
- Verify the DLL loaded successfully (no errors during NETLOAD)
- Check that the command name is typed correctly: `MODIFYPOINTSTYLE`

### Issue: No point styles appear in the dialog

**Solution**:
- Ensure your drawing has custom point styles defined
- Go to Settings > Point Styles in Civil 3D to create or import styles

### Issue: Plugin doesn't work with Civil 3D version

**Solution**:
- Verify you're using Civil 3D 2020-2023
- For other versions, you may need to rebuild with appropriate DLL references

## Development

### Architecture

The plugin follows a clean architecture pattern:

- **Commands**: Entry point for AutoCAD commands
- **Services**: Business logic and Civil 3D API operations
- **UI**: Windows Forms dialogs
- **Models**: Data transfer objects
- **Utilities**: Cross-cutting concerns (error handling, logging)

### Key Technologies

- C# .NET Framework 4.8
- AutoCAD .NET API
- Civil 3D .NET API
- Windows Forms

### API References

- `accoremgd.dll` - AutoCAD Core API
- `acdbmgd.dll` - AutoCAD Database API
- `acmgd.dll` - AutoCAD Managed API
- `AecBaseMgd.dll` - Civil 3D Base API
- `AeccDbMgd.dll` - Civil 3D Database API

## License

This project is provided as-is for use with Autodesk Civil 3D.

## Support

For issues or questions:
- Check the Troubleshooting section above
- Review the Civil 3D API documentation
- Verify all prerequisites are met

## Version History

### Version 1.0.0
- Initial release
- Support for Civil 3D 2020-2023
- Basic point style modification functionality
- Dialog-based style selection
