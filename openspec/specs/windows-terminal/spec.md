# Windows Terminal Launcher Specification

## Purpose

This specification defines the behavior of the "Carpeta de Trabajo" (Work Folder) access type in AccesosLauncher. When triggered, it SHALL launch Windows Terminal with multiple pre-configured tabs (lazygit, qwen, shell) in a specific working directory, reducing manual setup from ~15 clicks to 1 click.

## Requirements

### Requirement: RF-001 - New Access Type "Carpeta de Trabajo"
The system SHALL include "Carpeta de Trabajo" as a selectable option in the access type ComboBox.

#### Scenario: User selects "Carpeta de Trabajo" type
- GIVEN the user is on the "Agregar Acceso" (Add Access) form
- WHEN the user selects "Carpeta de Trabajo" from the Tipo de Acceso dropdown
- THEN the form SHALL display an additional directory path field

#### Scenario: Form displays route field for new type
- GIVEN the user selected "Carpeta de Trabajo" as the access type
- WHEN the form renders the input fields
- THEN a TextBox with FolderBrowserDialog button for path selection SHALL be visible

---

### Requirement: RF-002 - Required Directory Path Field
The system SHALL require a valid directory path for "Carpeta de Trabajo" access entries.

#### Scenario: User saves access with empty path
- GIVEN the user selected "Carpeta de Trabajo" type
- WHEN the user attempts to save without entering a path
- THEN the system SHALL prevent saving and display validation error

#### Scenario: User saves access with non-existent directory
- GIVEN the user entered a path in the route field
- WHEN the user attempts to save and the path does not exist on the filesystem
- THEN the system SHALL prevent saving and display validation error

---

### Requirement: RF-003 - Execute Windows Terminal with Configuration
The system SHALL execute wt.exe with tab configuration when the user clicks a "Carpeta de Trabajo" access.

#### Scenario: User clicks valid "Carpeta de Trabajo" access
- GIVEN a "Carpeta de Trabajo" access exists with a valid directory path
- WHEN the user clicks on the access item
- THEN Windows Terminal SHALL open with configured tabs

---

### Requirement: RF-004 - Read Tab Configuration from JSON
The system SHALL read tab configuration from the CarpetaDeTrabajoHerramientas.json file.

#### Scenario: JSON file exists with valid structure
- GIVEN CarpetaDeTrabajoHerramientas.json exists in the application directory
- WHEN the system reads the file
- THEN each tab SHALL be configured with properties: Orden, Title, TabColor, Parametro

#### Scenario: JSON file contains multiple tabs
- GIVEN the JSON file contains an array of tool configurations
- WHEN the configuration is loaded
- THEN the system SHALL create one tab per entry in the array

---

### Requirement: RF-005 - Shared Working Directory
All tabs SHALL open in the directory specified by the access path.

#### Scenario: Terminal tabs opened with directory
- GIVEN a "Carpeta de Trabajo" access points to directory "C:\Projects\MyApp"
- WHEN Windows Terminal is launched
- THEN every tab SHALL have "C:\Projects\MyApp" as its working directory

---

### Requirement: RF-006 - Tab Ordering by Orden Field
The system SHALL create tabs in ascending order by the Orden field.

#### Scenario: JSON contains tools with different Orden values
- GIVEN the JSON contains tools with Orden: 3, Orden: 1, Orden: 2
- WHEN the tabs are created
- THEN they SHALL appear in order: 1, 2, 3

---

### Requirement: RF-007 - Invalid JSON Error Handling
The system SHALL handle invalid JSON gracefully and use defaults.

#### Scenario: JSON file has malformed syntax
- GIVEN CarpetaDeTrabajoHerramientas.json exists but contains invalid JSON
- WHEN the system attempts to parse the file
- THEN the system SHALL use default tab configuration without crashing

#### Scenario: JSON file is empty
- GIVEN CarpetaDeTrabajoHerramientas.json exists but is empty
- WHEN the system reads the file
- THEN the system SHALL use default tab configuration

---

### Requirement: RF-008 - Non-existent Directory Error Handling
The system SHALL display a warning when the configured directory does not exist at execution time.

#### Scenario: Directory was deleted after access creation
- GIVEN a "Carpeta de Trabajo" access was saved with path "C:\Projects\OldProject"
- WHEN the user clicks the access and the directory no longer exists
- THEN a MessageBox SHALL appear with message "El directorio no existe: C:\Projects\OldProject" and Warning icon

---

### Requirement: RF-009 - Generic Error Handling
The system SHALL capture and display unexpected errors during execution.

#### Scenario: Unexpected exception during launch
- GIVEN the Windows Terminal launcher encounters an unhandled exception
- WHEN the error occurs
- THEN a MessageBox SHALL display with the error message and Error icon

---

### Requirement: RF-010 - Default Configuration when JSON Missing
The system SHALL use default tab configuration when the JSON file does not exist.

#### Scenario: JSON file does not exist
- GIVEN CarpetaDeTrabajoHerramientas.json is not present in the application directory
- WHEN the system loads the configuration
- THEN default tabs SHALL be created: lazygit, qwen, shell with their respective colors

#### Scenario: JSON deserialization returns null
- GIVEN the JSON file exists but deserialization yields null
- WHEN the configuration is loaded
- THEN default tab configuration SHALL be used
