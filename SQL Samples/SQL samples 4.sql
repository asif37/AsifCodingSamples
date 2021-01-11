CREATE FUNCTION [dbo].[fn_GetTimeHour1]
(
	-- Add the parameters for the function here
	@time1 time
	
)
RETURNS int
AS
BEGIN
	-- Declare the return variable here
	DECLARE @hour int
	--Declare @min int

	set @hour= Datepart(HOUR,@time1) -- Calculating Hours From CheckinTime
	--set @min=DATEPART(MINUTE,@time1)--Calculating Min From CheckinTime
	
	--if @min > 0
	--  Begin
	--  set @hour=@hour+1;
	-- 
    --  End

	-- Return the result of the function
	RETURN @hour

END
GO