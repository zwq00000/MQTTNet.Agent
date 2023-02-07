namespace MQTTnet.Agent.Tests;

public class IMessageAgentTests {
    private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageAgentTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }


    [Fact]
    public async void TestDispose() {
        var agent = factory.GetService<IMessageAgent>();
        Assert.NotNull(agent);
        var task = Task.Factory.StartNew(async ()=>{
            for(var i=0;i<10;i++){
                await agent.PublishAsync<string>("topic",i.ToString());
            }
            await Task.Delay(1000);
            agent.Dispose();
        });
        var reader =  await agent.GetChannelAsync<string>("topic");
        int count=0;
        await foreach(var item in reader.ReadAllAsync()){
            output.WriteLine(item.Payload);
            count++;
        }
        Assert.Equal(10,count);
    }
}

public class IMessageHubTests{
     private readonly ITestOutputHelper output;
    private readonly TestFactory factory;

    public IMessageHubTests(ITestOutputHelper outputHelper) {
        this.output = outputHelper;
        this.factory = new TestFactory();
    }


    [Fact]
    public async void TestDispose() {
        var agent = factory.GetService<IMessageHub>();
        Assert.NotNull(agent);
        var task = Task.Factory.StartNew(async ()=>{
            for(var i=0;i<10;i++){
                await agent.PublishAsync<string>("topic",i.ToString());
            }
            await Task.Delay(1000);
            agent.Dispose();
        });
        var subs =  await agent.SubscribeAsync<string>("topic");
        int count=0;
        subs.Subscribe(s=>{
            output.WriteLine(s.Payload);
            count++;
        });
        // await foreach(var item in reader.ReadAllAsync()){
        //     output.WriteLine(item.Payload);
        //     count++;
        // }
        Assert.Equal(10,count);
    }
}