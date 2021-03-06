


/****** Object:  StoredProcedure [dbo].[sp_deletearchiveddata]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	This procedure is used to delete archive data
--              with no associated Interaction
-- =============================================
CREATE PROCEDURE [dbo].[sp_deletearchiveddata]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	-- Delete Archived Employees 
	DECLARE @Employees TABLE(Id INT, UserId NVARCHAR(450))

	INSERT INTO @Employees
	SELECT [Id], [UserId] FROM [dbo].[Employee]
	WHERE [IsArchived] IS NOT NULL
	AND [IsArchived] = 'true' 
	AND [Id] NOT IN (SELECT [IdEmployee] FROM [dbo].[Interaction]
					 WHERE [IdEmployee] IS NOT NULL)

	-- DELETE Skillset
	DELETE FROM [dbo].[Employee_Reason]
	WHERE [IdEmployee] IN (SELECT Id FROM @Employees)

	-- DELETE Schedule Events
	DELETE FROM [dbo].[Schedule_Events]
	WHERE [IdSchedule] IN (SELECT A.[Id] FROM [dbo].[Schedule] A
						   JOIN @Employees B ON B.Id = A.[IdEmployee])

	-- DELETE Schedule
	DELETE FROM [dbo].[Schedule]
	WHERE [IdEmployee] IN (SELECT Id FROM @Employees)

	-- DELETE USER FROM ROLES
	DELETE FROM [dbo].[AspNetUserRoles]
	WHERE [UserId] IN (SELECT UserId FROM @Employees)

	-- DELETE Users
	DELETE FROM [dbo].[AspNetUsers]
	WHERE [Id] IN (SELECT UserId FROM @Employees)

	-- DELETE Employees
	DELETE FROM [dbo].[Employee]
	WHERE [Id] IN (SELECT Id FROM @Employees)

	-- Delete Archived Locations
	DELETE FROM [dbo].[Location]
	WHERE [IsArchived] IS NOT NULL
	AND [IsArchived] = 'true' 
	AND [Id] NOT IN (SELECT [IdLocation] FROM [dbo].[Interaction]
					 WHERE [IdLocation] IS NOT NULL)
	AND [Id] NOT IN (SELECT [IdLocation] FROM [dbo].[Employee])
	AND [Id] NOT IN (SELECT [IdAssignedLocation] FROM [dbo].[Employee])

	-- Delete Archived Reasons
	DECLARE @Reasons TABLE (Id INT)
	DECLARE @ReasonCombinations TABLE (IdCombination INT)
	DECLARE @ReasonId INT
	DECLARE @ReasonCharId varchar(10)
	DECLARE @InteractionHistoryIds TABLE (Id INT)

	INSERT INTO @Reasons
	SELECT [Id]  FROM [dbo].[Reason]
	WHERE [IsArchived] IS NOT NULL
	AND [IsArchived] = 'true' 
	AND [Id] NOT IN (SELECT [IdReason] FROM [dbo].[Interaction_Reason])

	-- DELETE ReasonCombinations
	--INSERT INTO @ReasonCombinations
	--SELECT [Id], [Description] 
	
		
		WHILE EXISTS (SELECT 1 FROM @Reasons)
		BEGIN
			 SELECT TOP 1 @ReasonId = Id FROM @Reasons 

			 SELECT @ReasonCharId = CONVERT(varchar(10), @ReasonId)

			 INSERT INTO @ReasonCombinations SELECT [Id] FROM [dbo].[ReasonCombination]
					WHERE CONTAINS([Description],  @ReasonCharId)

			 INSERT INTO @InteractionHistoryIds SELECT [Id] FROM [dbo].[InteractionHistory]
					WHERE CONTAINS([Reasons],  @ReasonCharId)

			 DELETE FROM @Reasons WHERE Id = @ReasonId 
			 
		END

	DELETE FROM [dbo].[ReasonCombination]
	WHERE [Id] IN (SELECT IdCombination FROM @ReasonCombinations)

		--DELETE Interaction History
	DECLARE @RangeEnd INT
	SELECT @RangeEnd =  RangeEnd FROM [dbo].[MaintenanceRanges] WHERE [Description]  like '%InteractionHistoryCleanDays%'
	
	DELETE FROM [dbo].InteractionHistory
	WHERE [Id] IN (SELECT Id FROM @InteractionHistoryIds)
	OR [CheckinDate] < = DATEADD(dd,-@RangeEnd, GETDATE())
	
	
	-- DELETE Employee_Reason records
	DELETE FROM [dbo].[Employee_Reason]
	WHERE [IdReason] IN (SELECT Id FROM @Reasons)

	DELETE FROM [dbo].[Reason]
	WHERE [Id] IN (SELECT Id FROM @Reasons)

	-- Delete Archived Categories
	DELETE FROM [dbo].[Category]
	WHERE [IsArchived] IS NOT NULL
	AND [IsArchived] = 'true' 
	AND [Id] NOT IN (SELECT [IdCategory] FROM [dbo].[Reason])

	-- Delete Archived Queues
	DELETE FROM [dbo].[Queue]
	WHERE [IsArchived] IS NOT NULL
	AND [IsArchived] = 'true' 
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[Category])
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[ActiveInteraction])
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[Interaction])
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[Employee])
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[Reason])
	AND [Id] NOT IN (SELECT [IdQueue] FROM [dbo].[Position])

	-- Delete schedule, schedule events and weekly comments older than option in MaintenanceRanges table
	DECLARE @OldSchedules TABLE (Id INT, ScheduleDate DATE)
	DECLARE @Date INT
	SELECT @Date =  RangeEnd FROM [dbo].[MaintenanceRanges] WHERE [Description]  like '%InteractionHistoryExpiryDays%'
	
	--Delete interactions
	DELETE FROM [dbo].[Interaction]
	WHERE [CheckinDate] < = DATEADD(dd, -@Date, GETDATE())

	INSERT INTO @OldSchedules
	SELECT [Id], [Date] FROM [dbo].[Schedule]
	WHERE [Date] < = DATEADD(dd, -@Date, GETDATE())

	--Delete Schedule Events
	DELETE FROM [dbo].[Schedule_Events]
	WHERE [IdSchedule] IN (SELECT Id FROM @OldSchedules)
	--Delete Schedule
	DELETE FROM [dbo].[Schedule]
	WHERE [Id] IN (SELECT Id FROM @OldSchedules)

	--DELETE Position and PositionAccessLevels
	DECLARE @Positions TABLE (Id INT)
	DECLARE @PositionId INT

	INSERT INTO @Positions
	SELECT [Id] FROM [dbo].[Position]
	WHERE [IsArchived] = 'true' 
	
    WHILE EXISTS (SELECT 1 FROM @Positions)
		BEGIN
			 SELECT TOP 1 @PositionId = Id FROM @Positions 

			 IF EXISTS ( SELECT TOP(1) [Id] FROM [dbo].[Employee]
						WHERE IdPosition = @PositionId 
						AND [Id] NOT IN (SELECT [Id] FROM @Employees
										WHERE [Id] IS NOT NULL))
						PRINT '0'
						ELSE
						--delete from  PositionAccessLevel
						DELETE FROM [dbo].[PositionAccessLevel]
						WHERE [IdPosition] = @PositionId
						--delete from Position
						DELETE FROM [dbo].[Position]
						WHERE [Id] = @PositionId

			 DELETE FROM @Positions WHERE Id = @PositionId
			 
		END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_employeematchreasons]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_employeematchreasons]
	@ReasonIds NVARCHAR(4000),
	@IdEmployee INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @Reasons TABLE (Id INT)

	INSERT INTO @Reasons
	SELECT * FROM [dbo].[fn_split](@ReasonIds, ',')

	DECLARE @MatchSkillset BIT = 'false'

	DECLARE @EmployeeReasons NVARCHAR(4000) = ''

	SELECT @EmployeeReasons = @EmployeeReasons + ',' + CAST([IdReason] AS NVARCHAR) FROM  [dbo].[Employee_Reason] A
	WHERE A.[IdEmployee] = @IdEmployee
	AND A.[IdReason] IN (SELECT B.Id FROM @Reasons B)
	ORDER BY A.[IdReason]

	SET @EmployeeReasons = SUBSTRING(@EmployeeReasons, 2, 4000)

	IF @ReasonIds <> '' AND @EmployeeReasons <> '' AND @ReasonIds = @EmployeeReasons
		BEGIN
			SET @MatchSkillset = 'true'
		END

	SELECT @MatchSkillset AS MatchSkillset
END
GO
/****** Object:  StoredProcedure [dbo].[sp_estimateservicetime]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_estimateservicetime]
	-- Add the parameters for the stored procedure here
	@ReasonIds NVARCHAR(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @Result TIME
	SET @Result = '00:00:00'

	-- Check if there is a combination
	DECLARE @CombinationAverageTime TIME = '00:00:00'
	
	SELECT @CombinationAverageTime = [AverageServiceTime] FROM [dbo].[ReasonCombination]
	WHERE [Description] = @ReasonIds

	IF @CombinationAverageTime IS NOT NULL AND @CombinationAverageTime <> '00:00:00'
		BEGIN
			SET @Result = @CombinationAverageTime
		END
	ELSE 
		BEGIN

			-- ELSE use the Reason table (AverageServiceTime column)
			DECLARE @Reasons TABLE (Id int)
			INSERT INTO @Reasons
			SELECT * FROM [dbo].[fn_split](@ReasonIds, ',')

			SELECT @Result = [dbo].[fn_timesum](@Result, [AverageServiceTime]) FROM [dbo].[Reason]
			WHERE [Id] IN (SELECT * FROM @Reasons)
		END

	-- Return the result of the function
	SELECT @Result
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_abandondetail]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_report_abandondetail]
	@StartDate DATE,
	@EndDate DATE, 
	@IdLocation INT,
	@IdQueue INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @ZeroTime TIME
	SET @ZeroTime = '00:00:00' 
	DECLARE @TotalInteractions INT = 0
	DECLARE @TotalAbandoned INT = 0
	DECLARE @TotalWithWait INT = 0
	DECLARE @AvgWaitTime TIME = '00:00:00'
	DECLARE @TotalWaitSuccess INT = 0


	DECLARE @ResultTable TABLE 
	(
		LocationDescription NVARCHAR(512),
		Engagement NVARCHAR(10),
		PersonName NVARCHAR(60),
		Account NVARCHAR(128),
		Reasons NVARCHAR(MAX),
		CheckInDate DATE,
		CheckInTime TIME,
		CheckInType NVARCHAR(512),
		EstimatedWaitTime TIME, 
		ActualWaitTime TIME,
		PersonInteractions INT,
		QueueDescription NVARCHAR(512),
		Comments NVARCHAR(512)
	)

	INSERT INTO @ResultTable
	SELECT  B.[Description], 
			C.[Engagement], 
			K.[FirstName] + ' ' + K.[LastName],
			C.AccountNumber, 
			[dbo].[fn_interactionreasons](A.[Id]), 
			A.[CheckInDate], 
			A.[CheckInTime], 
			D.[Type],
			[dbo].[fn_timediff](A.[CheckInTime], A.[EstimateServiceStart]),
			[dbo].[fn_timediff](A.[CheckInTime], A.[ServiceStart]),
			[dbo].[fn_personinteractions](A.[IdPerson]),
			E.[Description],
			ISNULL(A.[Comments], '')
	FROM	[dbo].[Interaction] A
			JOIN [dbo].[Location] B ON B.[Id] = A.[IdLocation]
			JOIN [dbo].[Person] C ON C.[Id] = A.[IdPerson]
			JOIN [dbo].[CheckInType] D ON D.[Id] = A.[IdCheckInType]
			JOIN [dbo].[PersonName] K On K.Id = A.IdPersonName
			LEFT JOIN [dbo].[Queue] E ON E.[Id] = A.[IdQueue]
	WHERE	A.[CheckInDate] >= @StartDate 
			AND A.[CheckInDate] <= @EndDate
			AND A.[ServiceEnd] = @ZeroTime
			AND (@IdLocation = 0 OR A.[IdLocation] = @IdLocation)
			AND (@IdQueue = 0 OR A.[IdQueue] = @IdQueue) 

	SELECT * FROM @ResultTable

	--Summary Section
	SET @TotalInteractions = 
		(SELECT COUNT(*) 
		FROM Interaction 
		WHERE [CheckInDate] >= @StartDate 
			AND [CheckInDate] <= @EndDate
			AND (@IdLocation = 0 OR [IdLocation] = @IdLocation)
			AND (@IdQueue = 0 OR [IdQueue] = @IdQueue))

	SELECT
		@TotalAbandoned = COUNT(*),
		@TotalWithWait =
			SUM(
				CASE
					WHEN ActualWaitTime > @ZeroTime THEN
						1
					ELSE 
						0 
				END),
		@AvgWaitTime =  CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, CASE WHEN ActualWaitTime > @ZeroTime THEN ActualWaitTime ELSE NULL END)), 0)),
		@TotalWaitSuccess = 
			SUM(
				CASE
					WHEN ActualWaitTime <= EstimatedWaitTime AND ActualWaitTime > @ZeroTime AND EstimatedWaitTime > @ZeroTime THEN
						1
					ELSE
						0
				END)
	FROM @ResultTable

	SELECT @TotalInteractions,
		@TotalAbandoned, 
		CAST(ROUND(100 * CAST(@TotalAbandoned AS FLOAT)/(CASE WHEN CAST(@TotalInteractions AS FLOAT)<>0 THEN CAST(@TotalInteractions AS FLOAT) ELSE 1 END),0) AS INT),		
		@AvgWaitTime,
		CAST(ROUND(100 * CAST(@TotalWaitSuccess AS FLOAT)/(CASE WHEN CAST(@TotalWithWait AS FLOAT)<>0 THEN CAST(@TotalWithWait AS FLOAT) ELSE 1 END),0) AS INT)

END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_employeeproductivitydaily]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_report_employeeproductivitydaily]
		@StartDate DATE,
		@EndDate DATE,
		@IdEmployee NVARCHAR(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;


DECLARE @Interaction TABLE
(
	MyDate DATE,
	EmployeeId INT,
	Completed INT,
	Assigned INT,
	ServiceTime TIME,
	AverageServiceTime TIME,
	SuccessCount INT
)

DECLARE @Schedule TABLE
(
	MyDate DATE,
	EmployeeId INT,
	ScheduleTime TIME,
	UnavailableTime TIME,
	AvailableTime TIME
)

DECLARE @Results TABLE
(
	Name NVARCHAR(MAX),
	Completed INT,
	Assigned INT,
	ScheduleTime TIME,
	UnavailableTime TIME,
	AvailableTime TIME,
	AvailableRatio INT,
	ServiceTime TIME,
	ServiceRatio INT,
	IdleTime TIME,
	IdleRatio INT,
	AverageServiceTime TIME,
	SuccessRatio INT,
	SuccessCount INT
)


DECLARE @ZeroTime TIME = '00:00:00'

INSERT INTO @Interaction
	SELECT
		I.CheckinDate,
		I.IdEmployee,
		--Completed
		COUNT(I.Id),
		--Assigned
		SUM(CASE
				WHEN I.IdAssignReason IS NOT NULL AND I.IdAssignReason <> 2 THEN
					1
				ELSE
					0
			END),
		--ServiceTime
		CONVERT(TIME,DATEADD(SECOND,SUM(DATEDIFF(SECOND,@ZeroTime,I.ServiceTime)),@ZeroTime)),
		--Average Service Time
		CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, ServiceTime)), 0)),
		--Success Count
		SUM(
			CASE
				WHEN I.ServiceTime <= I.EstimateServiceTime THEN
					1
				ELSE
					0
			END)
	FROM	Interaction I
	WHERE	I.CheckinDate BETWEEN @StartDate AND @EndDate
			AND I.IdEmployee IS NOT NULL
			AND I.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@IdEmployee, ','))
			AND I.ServiceTime <> @ZeroTime
	GROUP BY I.CheckinDate, I.IdEmployee
	ORDER BY I.CheckinDate, I.IdEmployee

INSERT INTO @Schedule
	SELECT
		S.Date,
		S.IdEmployee,
		--Schedule Time (minus offline time)
		CONVERT(TIME,DATEADD(SECOND,SUM(DATEDIFF(SECOND,@ZeroTime,
			CASE WHEN S.ScheduleDuration > S.ScheduleOffline THEN [dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration) ELSE @ZeroTime END)),@ZeroTime)),
		--Unavailable Time
		CONVERT(TIME,DATEADD(SECOND,SUM(DATEDIFF(SECOND,@ZeroTime,S.ScheduleUnavailable)),@ZeroTime)),
		--Available Time
		CONVERT(TIME,DATEADD(SECOND,SUM(DATEDIFF(SECOND,@ZeroTime,
			CASE
				WHEN S.ScheduleDuration > S.ScheduleOffline AND [dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration) > S.ScheduleUnavailable THEN
					[dbo].[fn_timediff](S.ScheduleUnavailable,[dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration))
				ELSE
					@ZeroTime
			END)),@ZeroTime))
	FROM	Schedule S
	WHERE	S.Date BETWEEN @StartDate AND @EndDate
			AND S.IdEmployee IS NOT NULL
			AND S.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@IdEmployee, ','))
			AND S.ScheduleDuration <> @ZeroTime
	GROUP BY S.Date, S.IdEmployee
	ORDER BY S.Date, S.IdEmployee

	--Results
	SELECT
		I.MyDate,
		E.FirstName + ' ' + E.LastName,
		I.Completed,
		I.Assigned,
		S.ScheduleTime,
		S.UnavailableTime,
		S.AvailableTime,
		--Available Ratio
		CASE 
			WHEN S.AvailableTime > @ZeroTime AND S.ScheduleTime >@ZeroTime THEN
				CAST(ROUND(100 * CAST(CONVERT(DATETIME,S.AvailableTime) AS FLOAT)/CAST(CONVERT(DATETIME,S.ScheduleTime) AS FLOAT),0) AS INT)
			ELSE
				0
		END,
		I.ServiceTime,
		--Service ratio
		CASE
			WHEN I.ServiceTime > @ZeroTime AND S.AvailableTime > @ZeroTime THEN
				CAST(ROUND(100 * CAST(CONVERT(DATETIME,I.ServiceTime) AS FLOAT)/CAST(CONVERT(DATETIME,S.AvailableTime) AS FLOAT),0) AS INT)
			ELSE
				0
		END,
		--Idle Time
		CASE
			WHEN S.AvailableTime > @zeroTime AND S.AvailableTime > I.ServiceTime THEN
				[dbo].[fn_timediff](I.ServiceTime,S.AvailableTime)
			ELSE
				@ZeroTime
		END,
		--Idle Ratio
		CASE
			WHEN S.AvailableTime > @zeroTime AND S.AvailableTime > I.ServiceTime THEN
				CAST(ROUND(100 * CAST(CONVERT(DATETIME,[dbo].[fn_timediff](I.ServiceTime,S.AvailableTime)) AS FLOAT)/CAST(CONVERT(DATETIME,S.AvailableTime) AS FLOAT),0) AS INT)
			ELSE
				0
		END,
		I.AverageServiceTime,
		--Success Ratio
		CASE 
			WHEN I.SuccessCount > 0 AND I.Completed > 0 THEN 
				CAST(ROUND(100 * CAST(I.SuccessCount AS FLOAT) / CAST(I.Completed AS FLOAT),0) AS INT)	
			ELSE 
				0 
		END,
		SuccessCount
	FROM @Interaction I
		JOIN @Schedule S ON S.MyDate = I.MyDate AND S.EmployeeId = I.EmployeeId
		JOIN Employee E ON E.Id = I.EmployeeId

	--Summary Section
	DECLARE @SummaryServiceTime INT,
			@SummaryCompleted INT,
			@SummarySuccessCount INT,
			@SummaryAvailableTime INT,
			@SummaryScheduleTime INT
	SELECT 
		@SummaryServiceTime = SUM(CAST(DATEDIFF(SECOND, @ZeroTime,I.ServiceTime) AS INT)),
		@SummaryCompleted =SUM(I.Completed),
		@SummarySuccessCount = SUM(I.SuccessCount),
		@SummaryAvailableTime = SUM(CAST(DATEDIFF(SECOND,@ZeroTime,S.AvailableTime) AS INT)),
		@SummaryScheduleTime = SUM(CAST(DATEDIFF(SECOND,@ZeroTime,S.ScheduleTime) AS INT))
	FROM @Interaction I
		JOIN @Schedule S ON S.MyDate = I.MyDate AND S.EmployeeId = I.EmployeeId

	--results
	SELECT
		--Available Ratio
		CASE
			WHEN @SummaryAvailableTime > 0 AND @SummaryScheduleTime > 0 THEN
				CAST(ROUND(100 * CAST(@SummaryAvailableTime AS FLOAT)/ CAST(@SummaryScheduleTime AS FLOAT),0) AS INT)
			ELSE
				0
		END,
		----Service / Available Ratio
		CASE
			--When ServiceTime and AvailableTime are available calculate Service Ratio
			WHEN @SummaryServiceTime > 0  THEN
				CASE 
					WHEN @SummaryServiceTime <= @SummaryAvailableTime THEN
						CAST(ROUND(100 * CAST(@SummaryServiceTime AS FLOAT)/CAST(@SummaryAvailableTime AS FLOAT),0) AS INT)
					ELSE --If ServiceTime exceeds Available Time cap the ratio at 100%
						100
				END
			ELSE 0
		END,
		--Average Service Time
		CONVERT(TIME(0), DATEADD(SECOND,@SummaryServiceTime/@SummaryCompleted,0)),
		--Service Success
		CAST(ROUND(100 * CAST(@SummarySuccessCount AS FLOAT)/CAST(@SummaryCompleted AS FLOAT),0) AS INT)

			

		
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_employeeproductivitysummary]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- Batch submitted through debugger: SQLQuery1.sql|7|0|C:\Users\taha.farhaj\AppData\Local\Temp\~vs7F2F.sql

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================

--  EXEC sp_report_employeeproductivitysummary @StartDate = '05/01/2017',@EndDate = '05/31/2017',@IdLocation = 40,@IdQueue = 1


CREATE PROCEDURE [dbo].[sp_report_employeeproductivitysummary]
	@StartDate DATE,
	@EndDate DATE,
	@IdEmployees NVARCHAR(MAX)
AS

	BEGIN
	
	DECLARE @ZeroTime TIME = '00:00:00'

	DECLARE @Interaction TABLE
	(
		IdEmployee INT,
		Completed INT,
		Assigned INT,
		SuccessCount INT,
		ServiceTimeSeconds INT,
		AverageServiceTime TIME
	)

	INSERT INTO @Interaction
	SELECT
		I.IdEmployee,
		COUNT(I.Id),
		SUM(CASE
				WHEN I.IdAssignReason IS NOT NULL AND I.IdAssignReason <> 2 THEN
					1
				ELSE
					0
			END),
		SUM(CASE
				WHEN I.ServiceTime <> @ZeroTime AND I.ServiceTime <= I.EstimateServiceTime THEN	
					1
				ELSE
					0
			END),
		SUM(CAST(
			DATEDIFF(SECOND, @ZeroTime,I.ServiceTime) AS INT)),
		CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, CASE WHEN I.ServiceTime <> @ZeroTime THEN I.ServiceTime ELSE NULL END)), 0))
	FROM	Interaction I 
	WHERE	I.CheckinDate BETWEEN @StartDate AND @EndDate
			AND I.IdEmployee IS NOT NULL
			AND I.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@IdEmployees, ','))
			AND I.ServiceTime <> @ZeroTime
	GROUP BY I.IdEmployee

	DECLARE @Schedule AS TABLE
	(
		IdEmployee INT,
		ScheduleTimeSeconds INT,
		AvailableTimeseconds INT
	)

	INSERT INTO @Schedule
	SELECT
		S.IdEmployee,
		--ScheduleTimeSeconds
		SUM(CAST(DATEDIFF(SECOND, @ZeroTime,
			CASE 
				WHEN S.ScheduleDuration > S.ScheduleOffline THEN 
					[dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration) 
				ELSE @ZeroTime 
			END) AS INT)),
		--AvailableTimeSeconds
		SUM(CAST(
			DATEDIFF(SECOND,@ZeroTime,
				CASE
					WHEN S.ScheduleDuration > S.ScheduleOffline AND [dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration) > S.ScheduleUnavailable THEN
						[dbo].[fn_timediff](S.ScheduleUnavailable,[dbo].[fn_timediff](S.ScheduleOffline,S.ScheduleDuration))
					ELSE
						@ZeroTime
				END
			) AS INT))
	FROM Schedule S
	WHERE S.Date BETWEEN @StartDate AND @EndDate
		AND S.IdEmployee IS NOT NULL
		AND S.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@IdEmployees, ','))
	GROUP BY S.IdEmployee

	--Results
	SELECT
		E.FirstName + ' ' + E.LastName,
		I.Completed,
		I.Assigned,
		[dbo].[fn_ConvertTimeToHHMMSS](S.ScheduleTimeSeconds,'s'),
		[dbo].[fn_ConvertTimeToHHMMSS](S.AvailableTimeSeconds,'s'),
		----AvailableRatio
		CASE 
			--If Schedule Available Time and Schedule Time are available then calculate ratio
			WHEN S.AvailableTimeSeconds > 0 AND S.ScheduleTimeSeconds > 0 THEN
				CAST(ROUND(100 * CAST(S.AvailableTimeSeconds AS FLOAT)/CAST(S.ScheduleTimeSeconds AS FLOAT), 0) AS INT)
			ELSE
				0
		END,
		----Service Time
		[dbo].[fn_ConvertTimeToHHMMSS](I.ServiceTimeSeconds,'s'),
		----Service / Available Ratio
		CASE
			--When ServiceTime and AvailableTime are available calculate Service Ratio
			WHEN I.ServiceTimeSeconds > 0  THEN
				CASE 
					WHEN I.ServiceTimeSeconds <= S.AvailableTimeSeconds THEN
						CAST(ROUND(100 * CAST(I.ServiceTimeSeconds AS FLOAT)/CAST(S.AvailableTimeSeconds AS FLOAT),0) AS INT)
					ELSE --If ServiceTime exceeds Available Time cap the ratio at 100%
						100
				END
			ELSE 0
		END,
		--Idle Time (Available Time - Service Time)
		CASE
			WHEN I.ServiceTimeSeconds < S.AvailableTimeSeconds THEN
				[dbo].[fn_ConvertTimeToHHMMSS](S.AvailableTimeSeconds - I.ServiceTimeSeconds,'s')
			ELSE
				'00:00:00'
		END,
		--Idle / Available Ratio
		CASE
			WHEN I.ServiceTimeSeconds < S.AvailableTimeSeconds THEN
				CAST(ROUND(100 * CAST(S.AvailableTimeSeconds - I.ServiceTimeSeconds AS FLOAT)/CAST(S.AvailableTimeseconds AS FLOAT),0) AS INT)
			ELSE
				0
		END,
		I.AverageServiceTime,
		--Service Success Rate
		CAST(ROUND(100 * CAST(I.SuccessCount AS FLOAT) / CAST(I.Completed AS FLOAT) , 0) AS INT)
				
	FROM @Interaction I
	JOIN @Schedule S ON I.IdEmployee = S.IdEmployee
	JOIN Employee E ON I.IdEmployee = E.Id
	WHERE I.ServiceTimeSeconds > 0 
		AND S.ScheduleTimeSeconds > 0


--Summary Section
	SELECT
		--Average Service Time
		CONVERT(TIME(0),DATEADD(SECOND,SUM(I.ServiceTimeSeconds)/SUM(I.Completed),0)),
		--Service Success
		CAST(ROUND(100 * CAST(SUM(I.SuccessCount) AS FLOAT)/CAST(SUM(I.Completed) AS FLOAT),0) AS INT)
	FROM @Interaction I

END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_LocationProductivity]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[sp_report_LocationProductivity]    
 @StartDate DATE,    
 @EndDate DATE,    
 @IdLocation nvarchar(100) = '0',    
 @IdQueue INT
AS

BEGIN
IF @IdLocation = '0'
 BEGIN
  SET @IdLocation = NULL
  SELECT @IdLocation = COALESCE(@IdLocation + ', ', '') + CAST(Id AS NVARCHAR(5)) FROM Location
 END
DECLARE @InteractionResult TABLE
	(
		InteractionCount DECIMAL,
		IdLocation INT,
		LocationName NVARCHAR(200),
		TotalInteraction INT,
		TotalAbandon INT,
		TotalAbandonTime INT,
		TotalMember INT,
		TotalRepeatMember INT,
		TotalVisitCount INT,
		TotalWaitTime INT,
		TotalWaitCount DECIMAL,
		CompleteInteraction DECIMAL,
		TotalServiceTime INT
	)
DECLARE @ZeroTime Time = '00:00:00'


INSERT INTO @InteractionResult
SELECT
	COUNT(*) AS InteractionCount,
	inte.IdLocation,
	(
		SELECT loc.Description FROM Location loc WHERE loc.Id = inte.IdLocation
	) AS LocationName,
	(
		SELECT COUNT(*)
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL AND inteInternal.ServiceEnd <> '00:00:00'
		AND inte.IdLocation = inteInternal.IdLocation
	) AS TotalInteraction,
	(
		SELECT COUNT(*)
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL AND inteInternal.ServiceEnd = '00:00:00'
		AND inte.IdLocation = inteInternal.IdLocation
	) AS TotalAbandon,
	(
		SELECT SUM(CAST(DATEDIFF(SECOND, 0, [dbo].fn_timediff(inteInternal.CheckinTime , inteInternal.ServiceStart)) AS INT))
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL AND inteInternal.ServiceEnd = '00:00:00'
		AND inte.IdLocation = inteInternal.IdLocation
	) AS TotalAbandonTime,
	(

		SELECT COUNT(DISTINCT(inteInternal.IdPerson))
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL
		AND inte.IdLocation = inteInternal.IdLocation
		
	) AS TotalMember,
	(
		SELECT COUNT(*) FROM (SELECT COUNT(*) AS RepeatPersons
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL
		AND inte.IdLocation = inteInternal.IdLocation
		GROUP BY inteInternal.IdPerson) inteInternal WHERE inteInternal.RepeatPersons > 1
	)AS TotalRepeatMember,
	(
		SELECT SUM(inteInternal.RepeatPersons) FROM (SELECT COUNT(*) AS RepeatPersons
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inteInternal.ServiceEnd IS NOT NULL
		AND inteInternal.IdLocation = inte.IdLocation
		GROUP BY inteInternal.IdPerson) inteInternal WHERE inteInternal.RepeatPersons > 1
	)AS TotalVisitCount,
	SUM(CAST(DATEDIFF(SECOND, 0, inte.WaitTime) AS INT)) AS TotalWaitTime,
	(
		SELECT COUNT(*)
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND WaitTime IS NOT NULL AND WaitTime <> '00:00:00'
		AND inte.IdLocation = inteInternal.IdLocation
	) AS TotalWaitCount,
	(
		SELECT COUNT(*)
		FROM Interaction inteInternal
		WHERE inteInternal.CheckinDate >= @StartDate AND inteInternal.CheckinDate <= @EndDate AND IdQueue = @IdQueue 
		AND inteInternal.ServiceEnd IS NOT NULL AND inteInternal.ServiceEnd <> '00:00:00'
		AND inte.IdLocation = inteInternal.IdLocation
	) AS CompleteInteraction,
	SUM(CAST(DATEDIFF(SECOND, 0, inte.ServiceTime) AS INT)) AS TotalServiceTime

FROM Interaction inte 
WHERE inte.IdLocation IN (SELECT * FROM dbo.fn_ConvertStringIntoTable(@IdLocation)) AND inte.CheckinDate >= @StartDate 
AND inte.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inte.ServiceEnd IS NOT NULL
GROUP BY inte.IdLocation


SELECT 
	LocationName, 
	TotalInteraction, 
	TotalAbandon, 
	CASE 
		WHEN TotalAbandon > 0 AND InteractionCount > 0 THEN
			CAST(ROUND(100 * TotalAbandon / InteractionCount, 0) AS INT)
		ELSE
			0
	END AS AbandonRatio,
	CASE 
		WHEN TotalAbandonTime > 0 AND TotalAbandon > 0 THEN
			[dbo].[fn_ConvertTimeFormatFromSeconds](TotalAbandonTime / TotalAbandon) 
		ELSE
			'00:00:00'
	END AS AvgAbandonTime,
	TotalMember,
	TotalRepeatMember, 
	ISNULL(TotalVisitCount, 0) AS RepeatVisit, 
	CASE 
		WHEN TotalVisitCount > 0 AND InteractionCount > 0 THEN
			CAST(ROUND(100 * TotalVisitCount / InteractionCount, 0) AS INT)
		ELSE 
			0
	END AS RepeatRatio,
	CASE 
		WHEN TotalWaitTime > 0 AND TotalWaitCount > 0 THEN 
			[dbo].[fn_ConvertTimeFormatFromSeconds](TotalWaitTime / TotalWaitCount) 
		ELSE
			'00:00:00'
	END AS AvgWaitTime,
	CASE 
		WHEN TotalServiceTime > 0 AND CompleteInteraction > 0  THEN
			[dbo].[fn_ConvertTimeFormatFromSeconds](TotalServiceTime / CompleteInteraction)
		ELSE
			'00:00:00'
	END AS AvgServiceTime

FROM @InteractionResult


-- Calculate Total Summary

SELECT (SELECT SUM(TotalVisitCount)/COUNT(*) FROM @InteractionResult) AS SummaryRepeatVisit,
CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, inte.ServiceTime)), 0)) AS SummaryAvgServices,
CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, inte.WaitTime)), 0)) AS SummaryAvgWait
FROM Interaction inte 
WHERE inte.IdLocation IN (SELECT * FROM dbo.fn_ConvertStringIntoTable(@IdLocation)) AND inte.CheckinDate >= @StartDate 
AND inte.CheckinDate <= @EndDate AND IdQueue = @IdQueue AND inte.ServiceEnd IS NOT NULL



END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_queuedaily]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_report_queuedaily]
	@Date DATE,
	@IdLocation INT,
	@IdQueue INT,
	@Positions NVARCHAR(MAX),
	@Employees NVARCHAR(MAX),
	@StartDate DATE,
	@EndDate DATE,
	@ReturnSummary BIT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.

	--This stored procedure returns results for one day at a time. 
	-- it is called multiple times in a loop from the Reporting Helper class
	-- to build information for a date range.
	SET NOCOUNT ON;

	DECLARE @ZeroTime TIME,
			@interactionCount INT,
			@Completed INT,
			@Abandoned INT,
			@AverageWaitTime TIME,
			@AverageServiceTime TIME,
			@WaitCount INT,
			@WaitSuccessRate INT,
			@ServiceTimeSuccessRate INT

	SET @ZeroTime = '00:00:00' 

	SET @interactionCount = 0
	SET @Completed = 0
	SET @Abandoned = 0
	SET @WaitCount = 0
	SET @AverageWaitTime = '00:00:00'
	SET @AverageServiceTime = '00:00:00'
	SET @WaitSuccessRate = 0
	SET @ServiceTimeSuccessRate = 0

	DECLARE @Interactions TABLE
	(
		ServiceStart TIME NULL,
		ServiceEnd TIME NULL,
		ServiceTime TIME NULL,
		WaitTime TIME NULL,
		CheckInTime TIME,
		IdEmployee INT,
		EstimateServiceStart TIME,
		EstimateServiceTime TIME
	)


	INSERT INTO @Interactions
	SELECT	[ServiceStart], 
			[ServiceEnd],
			[ServiceTime],
			[WaitTime], 
			[CheckInTime],
			[IdEmployee], 
			[EstimateServiceStart],
			[EstimateServiceTime]
	FROM	[dbo].[Interaction]
	WHERE	[CheckInDate] = @Date
			AND (@IdLocation = 0 OR IdLocation = @IdLocation)
			AND (@IdQueue = 0 OR IdQueue = @IdQueue)
			AND (@Employees = '' OR (IdEmployee IS NOT NULL AND IdEmployee IN (SELECT * FROM [dbo].[fn_split](@Employees, ','))))
			AND (@Positions = '' OR @Employees <> '' OR (@Employees = '' AND IdEmployee IS NOT NULL AND IdEmployee IN (SELECT Id FROM Employee WHERE(IdPosition IN (SELECT item FROM [dbo].[fn_split](@Positions, ','))))))

	-- All interactions
	SELECT @interactionCount = COUNT(*) FROM @Interactions

	--Members Served + Avergage Service Time
	SELECT	@Completed = COUNT(*),
			@AverageServiceTime = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, ServiceTime)), 0))	   
    FROM	@Interactions
	WHERE	ServiceTime <> @ZeroTime

	-- Abandoned
	SELECT	@Abandoned = COUNT(*) FROM @Interactions
	WHERE	ServiceEnd = @ZeroTime

	-- Average Wait Time
	SELECT 
			@AverageWaitTime = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, WaitTime)), 0))
	FROM	@Interactions
	WHERE	WaitTime <> @ZeroTime

	-- Wait Success Rate
	DECLARE @SuccessCount INT = 0

	SELECT	@WaitCount = COUNT(*),
			@SuccessCount = SUM(CASE WHEN ServiceStart <= EstimateServiceStart THEN 1 ELSE 0 END)
	FROM	@Interactions
	WHERE	WaitTime <> @ZeroTime

	IF @SuccessCount > 0 AND @WaitCount > 0
		BEGIN
			SET @WaitSuccessRate = ROUND(100 * CAST(@SuccessCount AS FLOAT) / CAST(@WaitCount AS FLOAT),0)
		END

	-- Service Time Success Rate
	SELECT	@SuccessCount = COUNT(*) 
	FROM	@Interactions
	WHERE	ServiceEnd <> @ZeroTime
			AND ServiceTime <= EstimateServiceTime

	IF @SuccessCount > 0 AND @Completed > 0
		BEGIN
			SET @ServiceTimeSuccessRate = ROUND(100 * CAST(@SuccessCount AS FLOAT) / CAST(@Completed AS FLOAT),0)
		END

	SELECT @interactionCount, @Completed, @Abandoned, @AverageWaitTime, @AverageServiceTime,    
		    @WaitSuccessRate, @ServiceTimeSuccessRate


	--Return Summary Results when @ReturnSummary is true. This variable is made true on the 1st hour result only

	IF @ReturnSummary = 1
		BEGIN

			DECLARE @SummaryInteractionCount INT,
					@SummaryCompleted INT,
					@SummaryAbandoned INT,
					@SummaryAverageWait TIME,
					@SummaryAverageService TIME,
					@SummaryWaitSuccessCount INT,
					@SummaryWaitCount INT,
					@SummaryWaitSuccessRate INT,
					@SummaryServiceSuccessCount INT,
					@SummaryServiceSuccessRate INT,
					@SummaryAbandonRate INT

			SELECT
				@SummaryInteractionCount = COUNT(*),
				@SummaryCompleted = 
					SUM(CASE
							WHEN ServiceEnd <> @ZeroTime THEN
								1
							ELSE
								0
						END),
				@SummaryWaitCount = 
					SUM(CASE
							WHEN WaitTime <> @ZeroTime THEN
								1
							ELSE
								0
						END),
				@SummaryAbandoned = 
					SUM(CASE
							WHEN ServiceEnd = @ZeroTime THEN
								1
							ELSE
								0
						END),
				@SummaryAverageWait = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, CASE WHEN WaitTime <> @ZeroTime THEN WaitTime ELSE NULL END)), 0)),
				@SummaryAverageService = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, CASE WHEN ServiceTime <> @ZeroTime THEN ServiceTime ELSE NULL END)), 0)),
				@SummaryWaitSuccessCount = 
					SUM(CASE
							WHEN WaitTime <> @ZeroTime AND ServiceStart <= EstimateServiceStart  THEN
								1
							ELSE
								0
					END),
				@SummaryServiceSuccessCount = 
					SUM(CASE
							WHEN ServiceTime <> @ZeroTime AND ServiceTime <= EstimateServiceTime THEN
								1
							ELSE	
								0
						END)
			FROM Interaction 
			WHERE CheckinDate >= @StartDate AND CheckinDate <= @EndDate
					AND (@IdLocation = 0 OR IdLocation = @IdLocation)
					AND (@IdQueue = 0 OR IdQueue = @IdQueue)
					AND (@Employees = '' OR (IdEmployee IS NOT NULL AND IdEmployee IN (SELECT * FROM [dbo].[fn_split](@Employees, ','))))
					AND (@Positions = '' OR @Employees <> '' OR (@Employees = '' AND IdEmployee IS NOT NULL AND IdEmployee IN (SELECT Id FROM Employee WHERE(IdPosition IN (SELECT item FROM [dbo].[fn_split](@Positions, ','))))))

				
			SELECT @SummaryWaitSuccessRate =
				CASE
					WHEN @SummaryWaitSuccessCount > 0 AND @SummaryWaitCount > 0 THEN
						 ROUND(100* CAST(@SummaryWaitSuccessCount AS FLOAT)/ CAST(@SummaryWaitCount AS FLOAT),0)
					ELSE
						0
				END
			SELECT @SummaryServiceSuccessRate = 
				CASE 
					WHEN @SummaryServiceSuccessCount > 0 AND @SummaryCompleted > 0 THEN
						ROUND( 100* CAST(@SummaryServiceSuccessCount AS FLOAT) / CAST(@SummaryCompleted AS FLOAT),0)
					ELSE
						0
				END
			SELECT @SummaryAbandonRate = 
				CASE
					WHEN @SummaryAbandoned > 0 AND @SummaryInteractionCount > 0 THEN
						ROUND(100* CAST(@SummaryAbandoned AS FLOAT)/CAST(@SummaryInteractionCount AS FLOAT),0)
					ELSE
						0
				END

			SELECT @SummaryAverageWait, @SummaryAverageService, @SummaryWaitSuccessRate, @SummaryServiceSuccessRate, @SummaryAbandonRate

		END
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_queuedetail]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_report_queuedetail]
	@StartDate DATE,
	@EndDate DATE,
	@IdLocation INT,
	@IdQueue INT,
	@Employees NVARCHAR(MAX),
	@Positions NVARCHAR(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
		DECLARE @ZeroTime TIME,
				@AverageWaitTime TIME,
				@AverageServiceTime TIME,
				@WaitCount INT,
				@WaitSuccessCount INT,
				@ServiceSuccessCount INT,
				@WaitSuccessRate INT,
				@ServiceSuccessRate INT,
				@Total INT

	SET @ZeroTime = '00:00:00'
	SET	@AverageWaitTime = '00:00:00'
	SET @AverageServiceTime = '00:00:00'
	SET @WaitCount = 0
	SET @WaitSuccessCount = 0
	SET @ServiceSuccessCount = 0
	SET @WaitSuccessRate = 0
	SET @ServiceSuccessRate = 0
	SET @Total = 0


	SELECT  
		L.[Description], 
		P.[Engagement], 
		PN.[FirstName] + ' ' + PN.[LastName],
		P.[AccountNumber],
		[dbo].[fn_interactionreasons](I.[Id]), 
		I.[CheckInDate], 
		I.[CheckInTime], 
		D.[Type],
		CASE
			WHEN EstimateServiceStart > CheckinTime THEN
				[dbo].[fn_timediff](CheckInTime,EstimateServiceStart)
			ELSE
				'00:00:00'
		END AS EstimateWait,
		I.[WaitTime],
		I.[EstimateServiceTime],
		I.[ServiceTime],
		I.[IdEmployee],
		E.[FirstName] + ' ' + E.[LastName] AS EmployeeName,
		[dbo].[fn_personinteractions](I.[IdPerson]) AS InteractionCount,
		I.[ServiceStart],
		CASE 
			WHEN IdAssignReason IS NOT NULL AND IdAssignReason <> 2 THEN
				'Y'
			ELSE
				'N'
		END AS Assigned,
		F.[Reason],
		G.[Description],
		I.[Comments],
		P.IsNonMember
	FROM Interaction I
		JOIN [dbo].[Location] L ON L.[Id] = I.[IdLocation]
		JOIN [dbo].[Queue] G ON G.[Id] = I.[IdQueue]
		JOIN [dbo].[Person] P ON P.[Id] = I.[IdPerson]
		JOIN [dbo].[PersonName] PN ON PN.Id = I.IdPersonName
		JOIN [dbo].[CheckInType] D ON D.[Id] = I.[IdCheckInType]
		LEFT JOIN [dbo].[Employee] E ON E.[Id] = I.[IdEmployee]
		LEFT JOIN [dbo].[AssignReason] F ON F.[Id] = I.[IdAssignReason]
	WHERE [CheckInDate] >= @StartDate 
		AND [CheckInDate] <= @EndDate
		AND [ServiceEnd] <> @ZeroTime
		AND (@IdLocation = 0 OR I.[IdLocation] = @IdLocation)
		AND (@IdQueue = 0 OR I.[IdQueue] = @IdQueue) 
		AND (@Employees = '' OR (I.IdEmployee IS NOT NULL AND I.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@Employees, ','))))
		AND (@Positions = '' OR @Employees <> '' OR (@Employees = '' AND I.IdEmployee IS NOT NULL AND I.IdEmployee IN (SELECT Id FROM Employee WHERE(IdPosition IN (SELECT item FROM [dbo].[fn_split](@Positions, ','))))))
	ORDER BY L.Description ASC,CheckInDate DESC


	--Summary Section

	SELECT 
		@Total = COUNT(*),
		@AverageWaitTime = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0,CASE WHEN WaitTime <> @ZeroTime THEN WaitTime ELSE Null END)), 0)),
		@AverageServiceTime = CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0,ServiceTime)), 0)),
		@WaitCount = SUM(CASE WHEN WaitTime <> @ZeroTime THEN 1 ELSE 0 END),
		@WaitSuccessCount = SUM(CASE WHEN WaitTime <> @ZeroTime AND ServiceStart <= EstimateServiceStart THEN 1 ELSE 0 END),
		@ServiceSuccessCount = SUM(CASE WHEN ServiceTime <= EstimateServiceTime THEN 1 ELSE 0 END)
	FROM Interaction
	WHERE [CheckInDate] >= @StartDate 
		AND [CheckInDate] <= @EndDate
		AND [ServiceEnd] <> @ZeroTime
		AND (@IdLocation = 0 OR IdLocation = @IdLocation)
		AND (@IdQueue = 0 OR IdQueue = @IdQueue) 
		AND (@Employees = '' OR (IdEmployee IS NOT NULL AND IdEmployee IN (SELECT * FROM [dbo].[fn_split](@Employees, ','))))
		AND (@Positions = '' OR @Employees <> '' OR (@Employees = '' AND IdEmployee IS NOT NULL AND IdEmployee IN (SELECT Id FROM Employee WHERE(IdPosition IN (SELECT item FROM [dbo].[fn_split](@Positions, ','))))))
		
	SET @WaitSuccessRate = ROUND(100 * CAST(@WaitSuccessCount AS FLOAT) / CAST(@WaitCount AS FLOAT),0)
	SET @ServiceSuccessRate = ROUND(100 * CAST(@ServiceSuccessCount AS FLOAT) / CAST(@Total AS FLOAT),0)

	SELECT	@AverageWaitTime,
			@AverageServiceTime,
			@WaitSuccessRate,
			@ServiceSuccessRate

END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_QueueProductivityCalendar]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure [dbo].[sp_report_QueueProductivityCalendar]
   @StartDate DATE,   
   @EndDate DATE,
   @IdLocation INT,    
   @IdQueue INT,
   @IdPositions NVARCHAR(100)
   AS
  
   BEGIN   
  -- SET NOCOUNT ON added to prevent extra result sets FROM  
 -- interfering with SELECT statements.  
  SET NOCOUNT ON;
  DECLARE @ScheduleList TABLE  
 (  
	 Id INT,
	 [Date] DATE null,  
	 ScheduleDuration TIME NULL,  
	 ScheduleUnavailable TIME NULL,  
	 IdEmployee INT NULL  
 )

 DECLARE @ScheduleEventList TABLE
 (
	[Date] DATE null,
	 ScheduleUnavailable INT NULL
 )
 DECLARE @ResultList TABLE  
 (  
	 [Date] DATE NULL,  
	 scheduleHours DECIMAL(8,2) NULL,  
	 interactionHours DECIMAL(8,2) NULL, 
	 Visits INT NULL,
	 DayColor FLOAT Null,
	 Forecast NVARCHAR(20),
	 [Description] NVARCHAR(150),
	 AvgWaitHours DECIMAL(8,2) NULL
 )  
 DECLARE @ScheduleAvailable TABLE
 (
	 [Date] DATE,
	 ScheduleAvailableMinute INT
 )
 DECLARE @TargetDateTable TABLE
 (
	 [Date] DATE,
	 Forecast NVARCHAR(20),
	 [Description] NVARCHAR(150)
 )
 DECLARE @Interaction TABLE
 (
	 Id INT,
	 CheckinDate DATE,
	 ServiceTime TIME,
	 WaitTime TIME,
	 IdEmployee INT
 )
 DECLARE @LocationHours TABLE
 (
	 DW INT,
	 StartTime TIME NULL,
	 EndTime TIME NULL
 )

	INSERT INTO @LocationHours SELECT 1, SundayStart, SundayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 2, MondayStart, MondayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 3, TuesdayStart, TuesdayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 4, WednesdayStart, WednesdayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 5, ThursdayStart, ThursdayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 6, FridayStart, FridayEnd FROM Location WHERE Id = @IdLocation;
	INSERT INTO @LocationHours SELECT 7, SaturdayStart, SaturdayEnd FROM Location WHERE Id = @IdLocation;

	IF @IdPositions IS NOT NULL AND @IdPositions <> ''
		BEGIN
			INSERT INTO @ScheduleList
			SELECT sch.Id,[Date],[dbo].[fn_timediff](sch.StartTime,sch.EndTime),ScheduleUnavailable,IdEmployee FROM Schedule sch
			JOIN Employee emp on sch.IdEmployee = emp.Id
			WHERE (DATE >= @StartDate AND DATE <= @EndDate) AND (sch.IdLocation = @IdLocation OR @IdLocation = 0) AND emp.IdQueue = @IdQueue AND (emp.IsArchived IS NULL OR emp.IsArchived = 0) AND emp.IdPosition in  (SELECT item FROM fn_split( @IdPositions,','))	
		END
	ELSE
		BEGIN
			INSERT INTO @ScheduleList
			SELECT sch.Id,[Date],[dbo].[fn_timediff](sch.StartTime,sch.EndTime),ISNULL(ScheduleUnavailable,'00:00:00'),IdEmployee FROM Schedule sch
			JOIN Employee emp on sch.IdEmployee = emp.Id
			WHERE (DATE >= @StartDate AND DATE <= @EndDate) AND (sch.IdLocation = @IdLocation OR @IdLocation = 0) AND emp.IdQueue = @IdQueue AND (emp.IsArchived IS NULL OR emp.IsArchived = 0) AND emp.IdPosition in  (SELECT Id from Position p where emp.IdPosition = p.Id)
		END

	--Setting Default StartDate AND EndDate
	IF @StartDate IS null AND @EndDate is null
		BEGIN
			SET @StartDate= DateAdd(MONTH,-1,GetDate());
			SET @EndDate=DATEADD(MONTH,1,GETDATE());
		End

	DECLARE @selectedEndDate DATE;
	IF @EndDate > GETDATE() AND @StartDate <= GETDATE()
	   SET @selectedEndDate=GETDATE();
	ELSE IF @EndDate<GetDate() AND @StartDate <=GETDATE()
	   SET @selectedEndDate = @EndDate;
	IF @selectedEndDate <= GETDATE()
	BEGIN
	 ----------------------------Filling interaction TABLE
		INSERT INTO @Interaction 
		SELECT Id,CheckinDate,ServiceTime,WaitTime,IdEmployee 
		FROM Interaction 
		WHERE IdLocation = @IdLocation AND IdQueue = @IdQueue AND CheckinDate >= @StartDate AND CheckinDate <= @selectedEndDate;

	----------------------Calculating Schedule Hour --------------------------
		
		INSERT INTO @ResultList([Date],scheduleHours) 
		SELECT sch.[Date], SUM(CAST(DATEDIFF(MINUTE, 0,sch.ScheduleDuration) AS INT)) / 60 
		FROM @ScheduleList sch
		WHERE sch.ScheduleDuration IS NOT NULL AND sch.ScheduleDuration <> '00:00:00' AND (sch.[Date] >= @StartDate  AND sch.[Date] <= @selectedEndDate )
		GROUP BY [Date]

		
		-- Get Schedule Event Sum all the time date wise

		INSERT INTO @ScheduleEventList (Date,ScheduleUnavailable)
		SELECT Date, SUM(ISNULL(CAST(DATEDIFF(MINUTE, 0, sch_Event.EventDuration) AS INT),0)) / 60
		FROM @ScheduleList sch
		JOIN Schedule_Events sch_Event on sch.Id = sch_Event.IdSchedule
		WHERE sch.ScheduleDuration IS NOT NULL AND sch.ScheduleDuration <> '00:00:00' AND (sch.[Date] >= @StartDate  AND sch.[Date] <= @selectedEndDate )
		GROUP BY [Date]


		-- Now bind Schedule and Shedule event

		update @ResultList Set scheduleHours = 
		scheduleHours - (ISNULL((SELECT ScheduleUnavailable from @ScheduleEventList sch_Event
					Where schOutter.Date = sch_Event.Date),0)
					)FROM @ResultList schOutter

	-----------------------------Calculating Ser,InteractionHour AND vists-----------------------------
		DECLARE @CurrentDate DATE;
		SET @CurrentDate=@StartDate;
		WHILE @CurrentDate <= @selectedEndDate
		BEGIN
			UPDATE @ResultList SET interactionHours = 
		   (SELECT CAST(SUM(DATEDIFF(SECOND,0, ServiceTime))AS DECIMAL(8,2)) / 3600 
		   FROM @Interaction 
		   WHERE CheckinDate = @CurrentDate), 
		   Visits = (SELECT COUNT(*) FROM @Interaction WHERE CheckinDate = @CurrentDate),
			  AvgWaitHours=
		   (SELECT CAST(SUM(DATEDIFF(SECOND,0,WaitTime))AS DECIMAL(8,2)) / 60
		   FROM @Interaction 
		   WHERE CheckinDate = @CurrentDate)/(SELECT COUNT(*) FROM @Interaction WHERE CheckinDate=@CurrentDate)
		   WHERE DATE = @CurrentDate;  	
			UPDATE @ResultList SET DayColor = (CAST(ISNULL(interactionHours,1) AS DECIMAL(10,2)) / (CASE WHEN scheduleHours = 0 THEN 1 WHEN scheduleHours <> 0 THEN scheduleHours End)) WHERE [Date] = @CurrentDate;
			SET @CurrentDate=DATEADD(DAY, 1, @CurrentDate); 
		END
	END

	DELETE @ScheduleEventList
	--Check for Future Dates
	IF @EndDate > getdate()
	BEGIN
		DECLARE @selectedStartDate DATE;
		IF @StartDate > GETDATE()
			SET @selectedStartDate = @StartDate;
		ELSE
			SET @selectedStartDate = DATEADD(DAY,1,GETDATE());
		
		--- ScheduleHours For Forecast
		INSERT INTO @ResultList([Date],scheduleHours) 
		SELECT sch.[Date], SUM(CAST(DATEDIFF(MINUTE, 0,sch.ScheduleDuration) AS INT)) / 60 FROM @ScheduleList sch
		WHERE sch.ScheduleDuration IS NOT NULL AND sch.ScheduleDuration <> '00:00:00' AND (sch.[Date] >= @selectedStartDate  AND sch.[Date] <= @EndDate )
		GROUP BY [Date]

		-- Get Schedule Event Sum all the time date wise

		INSERT INTO @ScheduleEventList (Date,ScheduleUnavailable)
		SELECT Date, SUM(ISNULL(CAST(DATEDIFF(MINUTE, 0, sch_Event.EventDuration) AS INT),0)) / 60
		FROM @ScheduleList sch
		JOIN Schedule_Events sch_Event on sch.Id = sch_Event.IdSchedule
		WHERE sch.ScheduleDuration IS NOT NULL AND sch.ScheduleDuration <> '00:00:00' AND (sch.[Date] >= @selectedStartDate  AND sch.[Date] <= @EndDate )
		GROUP BY [Date]

		-- Now bind Schedule and Shedule event

		update @ResultList Set scheduleHours = 
		scheduleHours - (ISNULL((SELECT ScheduleUnavailable from @ScheduleEventList sch_Event
			Where schOutter.Date = sch_Event.Date),0)
		)
	
		FROM @ResultList schOutter


		--SELECT * FROM @ResultList=-----------------------------------------------------------------

		 DECLARE @targetdate DATE;
		 SET @targetdate = @selectedStartDate;
		 -- Forecast of Week days for Future Day
	 
		 WHILE @targetdate <= @EndDate
			BEGIN
				DECLARE @COUNT INT = (Select Count(*) FROM @ResultList  Where Date = @targetdate)
				If(@COUNT = 0)
					INSERT INTO @ResultList (Date,scheduleHours,interactionHours,Visits,DayColor) values (@targetdate,0,0,0,0)

				DECLARE @Forecast  NVARCHAR(20);
				DECLARE @Forecast_Description NVARCHAR (150);
				SET @Forecast = (SELECT Forecast FROM Forecast
				WHERE IdLocation = @IdLocation AND IdQueue = @IdQueue AND IdFrequency = 1 AND [DayOfWeek] = (DATEPART(DW, @targetdate)-1));		
				UPDATE @ResultList SET Forecast= @Forecast WHERE [Date] = @targetdate; 
				SET @targetdate = DATEADD(DAY, 1, @targetdate);
			END
		
			-- Fill date Range table with future dates 
		
			DECLARE @DateRange TABLE
			(
				Dates date
			)


			INSERT INTO @DateRange
			SELECT DATEADD(DAY, nbr - 1, @StartDate)
			FROM (SELECT ROW_NUMBER() OVER (ORDER BY c.object_id ) AS Nbr FROM sys.columns c) nbrs
			WHERE   nbr - 1 <= DATEDIFF(DAY, @StartDate , @EndDate)

		---Monthly

		INSERT INTO @TargetDateTable 
		SELECT DR.Dates, F.Forecast, F.Description 
		FROM Forecast F JOIN @DateRange DR ON DATEPART(DAY,DR.Dates) = F.DayOfMonth 
		WHERE (F.IdLocation = @IdLocation AND F.IdQueue = @IdQueue AND F.IdFrequency = 2 AND DR.Dates >= @selectedStartDate AND DR.Dates <= @EndDate)
	
		-- check records is available in Result List Table
		Declare @countSpecific int;
		SET @countSpecific =  (SELECT COUNT(*) From @ResultList RS
		JOIN @TargetDateTable TDT ON RS.Date = TDT.Date)

		IF(@countSpecific = 0)
		BEGIN
		--Insert data in a result table for month forecast
			INSERT INTO @ResultList(Date,Forecast,Description) 
			SELECT * FROM @TargetDateTable 
		END
		ELSE
		BEGIN
		--update data in a result table for monthly forecast
			UPDATE T1 
			SET T1.Forecast = T2.Forecast, T1.Description = T2.Description
			FROM @ResultList AS T1 INNER JOIN 
			@TargetDateTable AS T2 ON T1.Date = T2.Date
		END
	
		DELETE @TargetDateTable;


		---Yearly
		INSERT INTO @TargetDateTable
		SELECT DR.Dates, F.Forecast, F.Description 
		FROM Forecast F JOIN @DateRange DR ON DR.Dates = F.Date
		WHERE (F.IdLocation = @IdLocation AND F.IdQueue = @IdQueue AND F.IdFrequency = 3 AND DR.Dates >= @selectedStartDate AND DR.Dates <= @EndDate)
	
		-- check records is available in Result List Table
		SET @countSpecific  = 0;
		SET @countSpecific =  (SELECT COUNT(*) From @ResultList RS JOIN @TargetDateTable TDT ON RS.Date = TDT.Date)

		IF(@countSpecific = 0)
		BEGIN
		--Insert data in a result table for yearly forecast
			INSERT INTO @ResultList(Date,Forecast,Description) 
			SELECT * FROM @TargetDateTable 
		END
		ELSE
		BEGIN
		--update data in a result table for yearly forecast
			UPDATE T1 
			SET T1.Forecast = T2.Forecast, T1.Description = T2.Description
			FROM @ResultList AS T1 INNER JOIN 
			@TargetDateTable AS T2 ON T1.Date = T2.Date
		END
	
		DELETE @TargetDateTable;

		---Yearly variable Then
		INSERT INTO @TargetDateTable 
		SELECT DR.Dates, F.Forecast, F.Description 
		FROM Forecast F 
		JOIN @DateRange DR ON (DATEPART(DW, DR.Dates) - 1 = F.[DayOfWeek] AND Month(DR.Dates)=F.Month AND dbo.fn_RecurrenceInMonth(DR.Dates, F.[DayOfWeek]) = CONVERT(int,F.ReccurenceDayOfWeek))
		WHERE (F.IdLocation = @IdLocation AND F.IdQueue = @IdQueue AND F.IdFrequency = 4 AND DR.Dates >= @selectedStartDate AND DR.Dates <= @EndDate);

		-- check records is available in Result List Table
		SET @countSpecific  = 0;
		SET @countSpecific =  (SELECT COUNT(*) From @ResultList RS JOIN @TargetDateTable TDT ON RS.Date = TDT.Date)

		IF(@countSpecific = 0)
		BEGIN
		--Insert data in a result table for yearly vailable forecast
			INSERT INTO @ResultList(Date, Forecast, Description) 
			SELECT * FROM @TargetDateTable 
		END
		ELSE
		BEGIN
		--update data in a result table for yearly vailable forecast
			UPDATE T1 
			SET T1.Forecast = T2.Forecast, T1.Description = T2.Description
			FROM @ResultList AS T1 INNER JOIN 
			@TargetDateTable AS T2 ON T1.Date = T2.Date
		END
	
		DELETE @TargetDateTable;
	END

SELECT * FROM @ResultList
	JOIN @LocationHours LH ON DATEPART(DW, Date) = LH.DW
	WHERE Date < GETDATE() OR (LH.StartTime <> LH.EndTime AND (Forecast <> '00:00:00' OR scheduleHours <> 0));

END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_ReasonSvcByemployee]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
    
CREATE PROCEDURE [dbo].[sp_report_ReasonSvcByemployee]    
 @StartDate DATE,    
 @EndDate DATE,    
 @IdLocation INT,    
 @IdQueue INT,    
 @IdsEmployees NVARCHAR(50),    
 @IdsPositions NVARCHAR(50)     
AS    
BEGIN    
 -- SET NOCOUNT ON added to prevent extra result sets from    
 -- interfering with SELECT statements.    
 SET NOCOUNT ON;    
    DECLARE @ZeroTime TIME 
	SET @ZeroTime = '00:00:00'
SELECT  
	I.IdEmployee,
	E.FirstName + ' ' + E.LastName EmployeeName,  
	RC.Title AS Reason,    
	Count(I.Id) InteractionCount,    
	CONVERT(TIME,DATEADD(SECOND, AVG(DATEDIFF(SECOND,0,ServiceTime)), 0))  AverageServiceTime,     
	MAX(RC.AverageServiceTime) EstimateServiceTime, 
    CAST(ROUND(CAST(SUM(
			CASE
				WHEN ServiceTime <= EstimateServiceTime THEN 
					1 
				ELSE 
					0 
			END
		)*100 AS FLOAT)/CAST(COUNT(Reasons) AS FLOAT),0) AS INT) ServiceSuccess    
FROM Interaction I 
	JOIN ReasonCombination RC ON I.Reasons = RC.Description   
	INNER JOIN Employee E ON E.Id = I.IdEmployee  
WHERE 
	(I.ServiceTime <> @ZeroTime)
	AND I.CheckinDate >= @StartDate     
	AND I.CheckinDate <= @EndDate  
	--filter by employees		
	AND ( 
			--If user entered employee(s) criteria then filter interactions by selected employees
			@IdsEmployees <> '' AND I.IdEmployee IN (SELECT item FROM fn_split( @IdsEmployees,',')) 
			--if user did not select employee(s) criteria then filter interactions by Position and Location and Queue
			OR (@IdsEmployees = '' AND I.IdEmployee IN (SELECT Id FROM Employee X WHERE (@IdsPositions= '' OR X.IdPosition IN (SELECT item FROM fn_split(@IdsPositions,','))) 
																						AND (X.IdAssignedLocation = @IdLocation  OR @IdLocation = 0) AND X.IdQueue = @IdQueue))
		)
GROUP BY I.IdEmployee,I.Reasons,RC.[Title], E.FirstName, E.LastName   
  
   
  --Calculate summary section
SELECT      
    CONVERT(time(0), DATEADD(SECOND, AVG(DATEDIFF(SECOND,0,ServiceTime)), 0))  AverageServiceTimeSummary,     
    CAST(ROUND(CAST(SUM(
			CASE
				WHEN ServiceTime <= EstimateServiceTime THEN 
					1 
				ELSE 
					0 
			END
		)*100 AS FLOAT)/CAST(COUNT(Reasons) AS FLOAT),0) AS INT) ServiceSuccessSummary    
FROM Interaction I 
	JOIN ReasonCombination RC ON I.Reasons = RC.Description   
	INNER JOIN Employee E ON E.Id = I.IdEmployee  
WHERE 
	(I.ServiceTime <> @ZeroTime)
	AND I.CheckinDate >= @StartDate     
	AND I.CheckinDate <= @EndDate  
	--filter by employees		
	AND ( 
			--If user entered employee(s) criteria then filter interactions by selected employees
			@IdsEmployees <> '' AND I.IdEmployee IN (SELECT item FROM fn_split( @IdsEmployees,',')) 
			--if user did not select employee(s) criteria then filter interactions by Position and Location and Queue
			OR (@IdsEmployees = '' AND I.IdEmployee IN (SELECT Id FROM Employee X WHERE (@IdsPositions= '' OR X.IdPosition IN (SELECT item FROM fn_split(@IdsPositions,','))) 
																						AND (X.IdAssignedLocation = @IdLocation  OR @IdLocation = 0) AND X.IdQueue = @IdQueue))
		)
  
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_ReasonSvcByLocation]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[sp_report_ReasonSvcByLocation]
	@StartDate DATE,
	@EndDate DATE,
	@IdLocation INT,
	@IdQueue INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	DECLARE @InteractionTotal INT
	DECLARE @SummaryServiceSuccessRate INT
	DECLARE @ServiceSuccessCount INT
	DECLARE @SummaryServiceTime TIME
	DECLARE @ZeroTime TIME

	SET @ZeroTime = '00:00:00'

	DECLARE @interactions TABLE
	(
		Id INT,
		Reasons NVARCHAR(MAX),
		IdLocation INT,
		IdQueue INT,
		ServiceTime TIME,
		EstimateServiceTime TIME
	)

	INSERT INTO @interactions
	SELECT	A.[Id], 
			B.[Title], 
			A.[IdLocation],
			A.[IdQueue],
			A.[ServiceTime],
			A.[EstimateServiceTime]
	FROM	[dbo].[Interaction] A 
	JOIN	[dbo].[ReasonCombination] B ON A.[Reasons] = B.[Description]
	WHERE	[CheckInDate] >= @StartDate
			AND [CheckInDate] <= @EndDate
			AND [ServiceTime] <> @ZeroTime
			AND (@IdLocation=0 OR [IdLocation] = @IdLocation)
			AND (@IdQueue=0 OR [IdQueue] = @IdQueue)



	DECLARE @GroupedResult TABLE
	(
		ReasonCodes NVARCHAR(MAX),
		Interactions INT,
		AverageServiceTime TIME,
		ServiceSuccessCount INT,
		ServiceSuccessPercent INT,
		EstimateServiceTime TIME
	)

	INSERT INTO @GroupedResult 
	SELECT	Reasons, 
			COUNT(*),
			CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, ServiceTime)), 0)),
			0, 
			0, 
			'00:00:00.000'
	FROM	@interactions
	GROUP BY Reasons

	UPDATE @GroupedResult
	SET ServiceSuccessCount = (SELECT COUNT(*) FROM @interactions
							   WHERE Reasons = ReasonCodes
							   AND ServiceTime <= EstimateServiceTime)

	UPDATE @GroupedResult 
	SET ServiceSuccessPercent =  ROUND(100 * CAST(ServiceSuccessCount AS FLOAT) / CAST(Interactions AS FLOAT),0)
	WHERE Interactions > 0 AND ServiceSuccessCount > 0

	UPDATE @GroupedResult
	SET EstimateServiceTime = B.AverageServiceTime
	FROM [dbo].[ReasonCombination] B
	WHERE ReasonCodes = Title

	SELECT * FROM @GroupedResult
	ORDER BY Interactions DESC

	--Summary
	SELECT @InteractionTotal = (SELECT COUNT(*) FROM @Interactions)
	SELECT @ServiceSuccessCount = (SELECT COUNT(*) FROM @interactions WHERE ServiceTime <= EstimateServiceTime)

	IF (@InteractionTotal > 0 AND @ServiceSuccessCount > 0)
			SET @SummaryServiceSuccessRate = ROUND(100 * CAST(@ServiceSuccessCount AS FLOAT)/CAST(@InteractionTotal AS FLOAT),0)
		ELSE
			SET @SummaryServiceSuccessRate = 0

	SET @SummaryServiceTime = (SELECT CONVERT(TIME, DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0, ServiceTime)), 0)) FROM @Interactions)

	SELECT @SummaryServiceSuccessRate,@SummaryServiceTime
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_repeatVisit]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_report_repeatVisit]  
 @IdLocation INT,  
 @IdQueue INT,  
 @StartDate DATE,      
 @EndDate DATE   
AS  
  
 BEGIN  
  
DECLARE @PersonGroup TABLE
	(
		IdPerson INT,
		Visits INT,
		ServiceTime TIME,
		idpersonname INT
	)

DECLARE @ZeroTime TIME

SET @ZeroTime = '00:00:00'
  
INSERT INTO @PersonGroup  
SELECT
	IdPerson,  
	COUNT(*),       
	DATEADD(SECOND, SUM(DATEDIFF(SECOND,0, Servicetime)),0),
	MAX(idpersonname)
FROM Interaction I  
WHERE (@IdLocation = 0 OR IdLocation = @IdLocation)  
	AND IdQueue = @IdQueue  
	AND CheckinDate >= @StartDate   
	AND CheckinDate <= @EndDate  
	AND IdPerson IS NOT NULL
GROUP BY IdPerson 
HAVING COUNT(*) > 1

SELECT 
	P.AccountNumber,  
	PN.FirstName + ' ' + PN.LastName,  
	P.Engagement,
	PG.ServiceTime,
	PG.Visits
FROM @PersonGroup PG
	JOIN Person P ON PG.IdPerson = P.Id
    JOIN PersonName PN ON PN.Id = idpersonname
ORDER BY PG.Visits DESC


--Calculate summary section
SELECT CAST(ROUND(CAST(SUM(Visits) AS FLOAT)/CAST(COUNT(*) AS FLOAT),0) AS INT),
	CONVERT(TIME,DATEADD(SECOND, AVG(DATEDIFF(SECOND, 0,ServiceTime)),0),0)
 FROM @PersonGroup
END
GO
/****** Object:  StoredProcedure [dbo].[sp_report_StaffEfficiencyAnalysis]    Script Date: 01/11/2021 09:30:21 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure [dbo].[sp_report_StaffEfficiencyAnalysis]  
 -- Add the parameters for the stored procedure here  
  @StartDate DATE,  
  @EndDate Date,  
  @IdLocation INT,  
  @IdQueue INT,  
  @IdEmployee NVARCHAR(MAX)  
As  
  
BEGIN  
DECLARE @ScheduleResult TABLE  
(  
 IdEmployee INT,  
 EmpName NVARCHAR(200),  
 SchDate DATE,  
 AvailableTime INT,  
 TotalAvailableTime INT  
)  
  
DECLARE @InteractionResult TABLE  
(  
 CheckinDate Date,  
 TotalWaitTime int,  
 TotalInteractionTime int,  
 TotalServiceTime int  
)  
  
  
WHILE @StartDate <= @EndDate --Start Date Loop  
BEGIN  
  
	INSERT INTO @ScheduleResult  
	SELECT  
		emp.Id,   
		emp.FirstName + ' ' + emp.LastName AS EmployeeName,  
		sch.Date,  
		CASE
			WHEN sch.ScheduleDuration > sch.ScheduleOffline  AND [dbo].[fn_timediff](sch.ScheduleOffline, sch.ScheduleDuration) > sch.ScheduleUnavailable THEN
				CAST(DATEDIFF(SECOND, 0, sch.ScheduleDuration) AS INT) - CAST(DATEDIFF(SECOND, 0, sch.ScheduleUnavailable) AS INT) - CAST(DATEDIFF(SECOND, 0, sch.ScheduleOffline) AS INT)
			ELSE
				0
		END AS AvailableTime,  
		(  
			SELECT  
				SUM(CAST(DATEDIFF(SECOND, 0, schAvailableTime.ScheduleDuration) AS INT) - CAST(DATEDIFF(SECOND, 0, schAvailableTime.ScheduleUnavailable) AS INT) - CAST(DATEDIFF(SECOND, 0, schAvailableTime.ScheduleOffline) AS INT))  
			FROM Schedule schAvailableTime 
			WHERE schAvailableTime.Date = @StartDate 
				AND schAvailableTime.IdLocation = @IdLocation 
				AND schAvailableTime.IdQueue = @IdQueue  
				AND (@IdEmployee = '' OR (schAvailableTime.IdEmployee IN (SELECT * FROM [dbo].[fn_split](@IdEmployee, ','))))
			GROUP BY schAvailableTime.Date  
		) AS TotalAvailableTime  
  
	FROM Schedule sch  
		JOIN Employee emp ON sch.IdEmployee = emp.Id  
	WHERE sch.Date = @StartDate  
		AND sch.IdLocation = @IdLocation 
		AND sch.IdQueue = @IdQueue 
		AND (@IdEmployee = '' OR (emp.Id IN (SELECT * FROM [dbo].[fn_split](@IdEmployee, ','))))
  
	INSERT INTO @InteractionResult  
	SELECT  
		I.CheckinDate,  
		SUM(CAST(DATEDIFF(SECOND, 0, I.WaitTime) AS INT)) AS TotalWaitTime,  
		0,  
		(
			SELECT 
				SUM(CAST(DATEDIFF(SECOND, 0, ServiceTime) AS INT)) 
			FROM Interaction 
			WHERE (ServiceTime IS NOT NULL AND ServiceTime <> '00:00:00') 
				AND CheckinDate = Cast(@StartDate AS DATE) 
				AND IdLocation = @IdLocation 
				AND IdQueue = @IdQueue
		) AS TotalServiceTime  
	FROM Interaction I  
	WHERE CheckinDate = Cast(@StartDate as date) 
		AND IdLocation = @IdLocation 
		AND IdQueue = @IdQueue  
	Group By I.CheckinDate  
  
	SET @StartDate = DATEADD(DAY, 1, @StartDate)  
  
End --End Date Loop  
  
SELECT   
  intResult.CheckinDate AS CheckinDate,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](intResult.TotalServiceTime) AS TotalServiceTime,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](intResult.TotalWaitTime) AS TotalWaitTime,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](intResult.TotalServiceTime + intResult.TotalWaitTime) AS TotalInteractionTime,  
  schResult.EmpName,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](schResult.AvailableTime) AS AvailableTime,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](schResult.TotalAvailableTime) AS  TotalAvailableTime,  
  [dbo].[fn_ConvertTimeFormatFromSeconds](schResult.TotalAvailableTime - (intResult.TotalServiceTime + intResult.TotalWaitTime)) AS StaffInteractionRatio   
  
FROM @ScheduleResult schResult  
	JOIN @InteractionResult intResult ON schResult.SchDate = intResult.CheckinDate 
ORDER BY schResult.SchDate
  
-- Calculate Total Summary  
  
SELECT  
	(SELECT [dbo].[fn_ConvertTimeFormatFromSeconds]( AVG(TotalAvailableTime))  
	FROM @ScheduleResult ) AS AvgAvailableTime,  
	(SELECT [dbo].[fn_ConvertTimeFormatFromSeconds]( AVG(TotalServiceTime))  
	FROM @InteractionResult)  AS AvgAvailableTime   
END
GO
