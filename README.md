# Expresso

Goal: Streamlined ETL with basic writing and conditions, completing the loop of simple business automation scenarios. Self-contained single-file executable. To trigger and automate workflows, call this program using CLI interface (details below).

Implements a subset of functionalities as guided by the [Parcel](https://github.com/Charles-Zhang-Parcel) experience.

Supports ODBC and Microsoft Analysis Service.

## Usage

Has three launch modes:

1. Launch executable directly to enter GUI mode; This mode allows authoring new contents.
2. Launch executable with a workflow file; This mode allow interactive input specification.
3. Launch executable with a workflow file and all variable names and values. This executes the workflow in headless mode.

### Expresso Best Practice

**Namving Conventions**

Generally, don''t use space and special symbols in names.

* Variables: Use `lowerCamelCase` for variable names.
* Readers: Use `CamelCase` for reader names.

### Application Workflow Architectures

#### Readers-Variables-Conditions-Writers

Standard ETL without using processors.

#### Readers-Processors-Conditions

Use processors exclusively to chain operations.

## Development

**Implementation Requirement**

To ensure sufficiency, no more features will be added beyond existing functionalities. The goal is to be efficient, robust and usable, not well-rounded or general purpose.

## Remarks

* Components: SQLite
* V1 intermediate data transmission format: CSV