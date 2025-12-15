using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SOPServer.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SeedFullBodyValidationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seed VALIDATE_FULLBODY_PROMPT (Type = 11)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AISettings WHERE Type = 11)
                BEGIN
                    INSERT INTO AISettings (Name, Value, Type, CreatedDate, IsDeleted)
                    VALUES (
                        'Full Body Validation Prompt',
                        'Analyze the provided image and determine if it is a valid full body image suitable for virtual try-on.

VALIDATION CRITERIA:
1. The image must show a complete human body from head to toe (or at least from shoulders to feet)
2. The person should be standing in a relatively neutral pose
3. The body should be clearly visible without major obstructions
4. The image should have reasonable lighting and clarity
5. The person should be wearing clothes (not nude)
6. The background should not significantly obstruct the body view

RESPOND IN JSON FORMAT:
{
  ""isValid"": true/false,
  ""message"": ""Brief explanation of why the image is valid or invalid for virtual try-on""
}

If the image is NOT a full body image or does not meet the criteria, set isValid to false and explain why.
If the image IS a valid full body image suitable for virtual try-on, set isValid to true.',
                        11,
                        GETUTCDATE(),
                        0
                    );
                END
                ELSE
                BEGIN
                    UPDATE AISettings SET Name = 'Full Body Validation Prompt' WHERE Type = 11;
                END
            ");

            // Seed MODEL_FULLBODY_VALIDATION (Type = 12) - Same model as MODEL_SUGGESTION
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM AISettings WHERE Type = 12)
                BEGIN
                    INSERT INTO AISettings (Name, Value, Type, CreatedDate, IsDeleted)
                    VALUES (
                        'Full Body Validation Model',
                        'gemini-2.0-flash',
                        12,
                        GETUTCDATE(),
                        0
                    );
                END
                ELSE
                BEGIN
                    UPDATE AISettings SET Name = 'Full Body Validation Model' WHERE Type = 12;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM AISettings WHERE Type = 11");
            migrationBuilder.Sql("DELETE FROM AISettings WHERE Type = 12");
        }
    }
}
