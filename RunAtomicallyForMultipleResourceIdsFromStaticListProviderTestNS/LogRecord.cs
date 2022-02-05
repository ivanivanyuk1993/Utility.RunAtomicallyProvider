using System;

namespace RunAtomicallyForMultipleResourceIdsFromStaticListProviderTestNS;

public class LogRecord<TResourceId>
{
    public readonly LogRecordTypeEnum LogRecordTypeEnum;
    public readonly TResourceId[] ResourceIdList;
    public readonly DateTimeOffset DateTimeOffset = DateTimeOffset.UtcNow;

    private LogRecord(
        LogRecordTypeEnum logRecordTypeEnum,
        TResourceId[] resourceIdList
    )
    {
        LogRecordTypeEnum = logRecordTypeEnum;
        ResourceIdList = resourceIdList;
    }

    public static LogRecord<TResourceId> CreateFinishRecord(TResourceId[] resourceIdList)
    {
        return new LogRecord<TResourceId>(
            logRecordTypeEnum: LogRecordTypeEnum.Finish,
            resourceIdList: resourceIdList
        );
    }

    public static LogRecord<TResourceId> CreateStartRecord(TResourceId[] resourceIdList)
    {
        return new LogRecord<TResourceId>(
            logRecordTypeEnum: LogRecordTypeEnum.Start,
            resourceIdList: resourceIdList
        );
    }
}