CREATE FUNCTION [dbo].[fn_employeequeueid]
(
	-- Add the parameters for the function here
	@IdEmployee INT
)
RETURNS INT
AS
BEGIN
	-- Declare the return variable here
	DECLARE @QueueId INT, @ReasonId INT

	-- Add the T-SQL statements to compute the return value here
	SELECT TOP 1 @ReasonId = [IdReason] FROM [dbo].[Employee_Reason]
	WHERE [IdEmployee] = @IdEmployee
	AND [IdReason] IN (SELECT [Id] FROM [dbo].[Reason] 
					   WHERE [IsArchived] IS NULL 
					   OR ([IsArchived] IS NOT NULL AND [IsArchived] = 'false'))

	SELECT TOP 1 @QueueId = A.[IdQueue] FROM [dbo].[Category] A
	JOIN [dbo].[Reason] B ON B.[IdCategory] = A.[Id]
	WHERE B.[Id] = @ReasonId

	-- Return the result of the function
	RETURN @QueueId

END
GO