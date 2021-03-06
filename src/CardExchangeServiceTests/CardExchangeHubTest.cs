using System;
using CardExchangeService;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using FluentAssertions;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace CardExchangeServiceTests
{
    public class CardExchangeHubTest
    {
        private const string DeviceId1 = "d77b8214 - f7de - 4405 - abda - e87cfa05abac";
        private const string DeviceId2 = "d77b8214 - f7de - 4405 - abda - e87cfa05abaa";
        private const double Latitude1 = 12.466561146;
        private const double LatitudeIn2 = 12.466561156;
        private const double Longitude = -34.405804850;
        private string _bse64StringImage1;
        private string _bse64StringImage2;

        private readonly HttpClient httpClient;

        private readonly string connectionUrl;
        // private const string host = "https://vswap-dev.smef.io";
        private const string host = "http://localhost:5000";

        public CardExchangeHubTest()
        {
            connectionUrl = host + "/swaphub";
            httpClient = new HttpClient();
            InitTestImageString();
        }
        private void InitTestImageString()
        {
            Byte[] bytes = File.ReadAllBytes("../../../img/1.jpg");
            _bse64StringImage1 = @"data:image/jpg;base64," + Convert.ToBase64String(bytes);
            bytes = File.ReadAllBytes("../../../img/2.jpg");
            _bse64StringImage2 = @"data:image/jpg;base64," + Convert.ToBase64String(bytes);
        }
        private byte[] GetImageBytes(string bse64StringImage)
        {
            var imageInfo = bse64StringImage.Split(',');

            if (imageInfo.Length < 2)
                return null;

            return Convert.FromBase64String(imageInfo[1]);
        }

        private async Task<byte[]> CallGetRequest(string relUrl)
        {
            return await httpClient.GetByteArrayAsync(host + relUrl);
        }


        private HubConnection CreateConnection()
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .Build();
            //connection.HandshakeTimeout = new TimeSpan(0, 0, 0, 0, 60000);
            //connection.KeepAliveInterval = new TimeSpan(0, 0, 0, 0, 60000);
            //connection.ServerTimeout = new TimeSpan(0, 0, 0, 0, 120000);

            return connection;
        }

        [Fact]
        public async void ConnectionTest_SubscribeUpdate2Subscribers_SubscribedCalled()
        {
            var connection1 = CreateConnection();
            var connection2 = CreateConnection();

            bool isSubscribedCalled1 = false;
            IEnumerable<string> resPeers1 = new List<string>();
            connection1.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled1 = true;
                resPeers1 = peers;
            });
            bool isUpdatedCalled1 = false;
            connection1.On(nameof(ICardExchangeClient.Updated), (IEnumerable<string> peers) =>
            {
                isUpdatedCalled1 = true;
                resPeers1 = peers;
            });

            bool isSubscribedCalled2 = false;
            IEnumerable<string> resPeers2 = new List<string>();
            connection2.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled2 = true;
                resPeers2 = peers;
            });


            bool isUpdatedCalled2 = false;
            connection2.On(nameof(ICardExchangeClient.Updated), (IEnumerable<string> peers) =>
            {
                isUpdatedCalled2 = true;
                resPeers2 = peers;
            });

            await connection1.StartAsync().ContinueWith(x =>
            {
                if (connection1.State == HubConnectionState.Connected)
                {
                    connection1.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await connection2.StartAsync().ContinueWith(x =>
            {
                if (connection2.State == HubConnectionState.Connected)
                {
                    connection2.SendAsync("Subscribe", DeviceId2, Longitude, LatitudeIn2, "displayName2", _bse64StringImage2);

                }
            });

            await Task.Delay(2000);

            await connection1.SendAsync("Update", DeviceId1, Longitude, Latitude1, "displayName1");
            await connection2.SendAsync("Update", DeviceId2, Longitude, LatitudeIn2, "displayName2");

            await Task.Delay(2000);

            await connection1.SendAsync("Update", DeviceId1, Longitude, Latitude1, "displayName1");
            await connection2.SendAsync("Update", DeviceId2, Longitude, LatitudeIn2, "displayName2");

            await Task.Delay(2000);

            isSubscribedCalled1.Should().BeTrue();
            isUpdatedCalled1.Should().BeTrue();
            resPeers1.Should().NotBeNull();
            resPeers1.Count().Should().Be(1);
            var data1 = JsonConvert.DeserializeObject<SubscriptionData>(((List<string>)resPeers1)[0]);
            data1.Should().NotBeNull();
            data1.Latitude.Should().NotBe(0);
            data1.Longitude.Should().NotBe(0);
            data1.DeviceId.Should().Be(DeviceId2);
            data1.DisplayName.Should().Be("displayName2");
            data1.ThumbnailUrl.Should().NotBeNullOrEmpty();


            isSubscribedCalled2.Should().BeTrue();
            isUpdatedCalled2.Should().BeTrue();
            resPeers2.Should().NotBeNull();
            var data2 = JsonConvert.DeserializeObject<SubscriptionData>(((List<string>)resPeers2)[0]);
            data2.Should().NotBeNull();
            data2.Latitude.Should().NotBe(0);
            data2.Longitude.Should().NotBe(0);
            data2.DeviceId.Should().Be(DeviceId1);
            data2.DisplayName.Should().Be("displayName1");
            data2.ThumbnailUrl.Should().NotBeNullOrEmpty();

            var getBytes = await CallGetRequest(data1.ThumbnailUrl);
            //var sendBytes = GetImageBytes(_bse64StringImage2);
            File.WriteAllBytes("../../../img/3.jpg", getBytes);
            // getBytes.Should().BeEquivalentTo(sendBytes);

            getBytes = await CallGetRequest(data2.ThumbnailUrl);
            //var sendBytes = GetImageBytes(_bse64StringImage2);
            File.WriteAllBytes("../../../img/4.jpg", getBytes);
        }

        [Fact]
        public async void ConnectionTest_Subscribe2Subscribers_SubscribedCalled()
        {
            var connection1 = CreateConnection();
            var connection2 = CreateConnection();

            bool isSubscribedCalled1 = false;
            IEnumerable<string> resPeers1 = new List<string>();
            connection1.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled1 = true;
                resPeers1 = peers;
            });

            bool isSubscribedCalled2 = false;
            IEnumerable<string> resPeers2 = new List<string>();
            connection2.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled2 = true;
                resPeers2 = peers;
            });

            await connection1.StartAsync().ContinueWith(async x =>
            {
                if (connection1.State == HubConnectionState.Connected)
                {
                    await connection1.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await Task.Delay(1000);

            await connection2.StartAsync().ContinueWith(async x =>
            {
                if (connection2.State == HubConnectionState.Connected)
                {
                    await connection2.SendAsync("Subscribe", DeviceId2, Longitude, LatitudeIn2, "displayName2", _bse64StringImage2);
                }
            });

            await Task.Delay(3000);

            isSubscribedCalled1.Should().BeTrue();
            resPeers1.Should().NotBeNull();
            resPeers1.Count().Should().Be(0);

            isSubscribedCalled2.Should().BeTrue();
            resPeers2.Should().NotBeNull();
            resPeers2.Count().Should().Be(1);
            var data2 = JsonConvert.DeserializeObject<SubscriptionData>(((List<string>)resPeers2)[0]);
            data2.Should().NotBeNull();
            data2.Latitude.Should().NotBe(0);
            data2.Longitude.Should().NotBe(0);
            data2.DeviceId.Should().Be(DeviceId1);
            data2.DisplayName.Should().Be("displayName1");
            data2.ThumbnailUrl.Should().NotBeNullOrEmpty();

            var getBytes = await CallGetRequest(data2.ThumbnailUrl);
            File.WriteAllBytes("../../../img/4.jpg", getBytes);
        }

        [Fact]
        public async void ConnectionTest_Subscribe_SubscribedCalled()
        {
            var connection = CreateConnection();

            bool isSubscribedCalled = false;
            IEnumerable<string> resPeers = new List<string>();
            connection.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled = true;
                resPeers = peers;
            });

            await connection.StartAsync().ContinueWith(x =>
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    connection.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await Task.Delay(2000);

            isSubscribedCalled.Should().BeTrue();
            resPeers.Should().NotBeNull();
            resPeers.Count().Should().Be(0);
        }

        [Fact]
        public async void ConnectionTest_UnSubscribe_UnSubscribedCalled()
        {
            var connection = CreateConnection();

            bool isSubscribedCalled = false;
            IEnumerable<string> resPeers = new List<string>();
            connection.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled = true;
                resPeers = peers;
            });

            bool isUnSubscribedCalled = false;
            string statusMessageUnSubscribed = String.Empty;
            connection.On(nameof(ICardExchangeClient.Unsubscribed), (string statusMessage) =>
            {
                isUnSubscribedCalled = true;
                statusMessageUnSubscribed = statusMessage;
            });

            await connection.StartAsync().ContinueWith(async x =>
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    await connection.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await Task.Delay(2000);

            isSubscribedCalled.Should().BeTrue();
            resPeers.Should().NotBeNull();
            resPeers.Count().Should().Be(0);

            await connection.SendAsync("Unsubscribe", DeviceId1);

            await Task.Delay(2000);

            isUnSubscribedCalled.Should().BeTrue();
            statusMessageUnSubscribed.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async void ConnectionTest_Subscribe2Subscribers_ManyUpdates_NoFallOuts()
        {
            var connection1 = CreateConnection();
            var connection2 = CreateConnection();

            bool isSubscribedCalled1 = false;
            IEnumerable<string> resPeers1 = new List<string>();
            connection1.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled1 = true;
                resPeers1 = peers;
            });
            bool isUpdatedCalled1 = false;
            int iCallUpdate1 = 0;
            connection1.On(nameof(ICardExchangeClient.Updated), (IEnumerable<string> peers) =>
            {
                iCallUpdate1++;
                isUpdatedCalled1 = true;
                resPeers1 = peers;
            });

            bool isSubscribedCalled2 = false;
            IEnumerable<string> resPeers2 = new List<string>();
            connection2.On(nameof(ICardExchangeClient.Subscribed), (IEnumerable<string> peers) =>
            {
                isSubscribedCalled2 = true;
                resPeers2 = peers;
            });


            bool isUpdatedCalled2 = false;
            int iCallUpdate2 = 0;
            connection2.On(nameof(ICardExchangeClient.Updated), (IEnumerable<string> peers) =>
            {
                iCallUpdate2++;
                isUpdatedCalled2 = true;
                resPeers2 = peers;
            });

            await connection1.StartAsync().ContinueWith(x =>
            {
                if (connection1.State == HubConnectionState.Connected)
                {
                    connection1.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await connection2.StartAsync().ContinueWith(x =>
            {
                if (connection2.State == HubConnectionState.Connected)
                {
                    connection2.SendAsync("Subscribe", DeviceId2, Longitude, LatitudeIn2, "displayName2", _bse64StringImage2);

                }
            });

            for (int i = 0; i < 50; i++)
            {
                connection1.SendAsync("Update", DeviceId1, Longitude, Latitude1, "displayName1");
            }

            for (int i = 0; i < 50; i++)
            {
                connection2.SendAsync("Update", DeviceId2, Longitude, LatitudeIn2, "displayName2");
            }

            await Task.Delay(8000);

            iCallUpdate1.Should().Be(50);
            isSubscribedCalled1.Should().BeTrue();
            isUpdatedCalled1.Should().BeTrue();
            resPeers1.Should().NotBeNull();
            resPeers1.Count().Should().Be(1);
            var data1 = JsonConvert.DeserializeObject<SubscriptionData>(((List<string>)resPeers1)[0]);
            data1.Should().NotBeNull();
            data1.Latitude.Should().NotBe(0);
            data1.Longitude.Should().NotBe(0);
            data1.DeviceId.Should().Be(DeviceId2);
            data1.DisplayName.Should().Be("displayName2");
            data1.ThumbnailUrl.Should().NotBeNullOrEmpty();

            iCallUpdate2.Should().Be(50);
            isSubscribedCalled2.Should().BeTrue();
            isUpdatedCalled2.Should().BeTrue();
            resPeers2.Should().NotBeNull();
            var data2 = JsonConvert.DeserializeObject<SubscriptionData>(((List<string>)resPeers2)[0]);
            data2.Should().NotBeNull();
            data2.Latitude.Should().NotBe(0);
            data2.Longitude.Should().NotBe(0);
            data2.DeviceId.Should().Be(DeviceId1);
            data2.DisplayName.Should().Be("displayName1");
            data2.ThumbnailUrl.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async void ConnectionTest_Swap2Subscribers_SubscribedCalled()
        {
            //Connection
            var connection1 = CreateConnection();
            var connection2 = CreateConnection();

            await connection1.StartAsync().ContinueWith(x =>
            {
                if (connection1.State == HubConnectionState.Connected)
                {
                    connection1.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                }
            });

            await connection2.StartAsync().ContinueWith(x =>
            {
                if (connection2.State == HubConnectionState.Connected)
                {
                    connection2.SendAsync("Subscribe", DeviceId2, Longitude, LatitudeIn2, "displayName2", _bse64StringImage2);

                }
            });


            // Events to handle
            //Connection 1
            string deviceIdReqCon1 = string.Empty;
            string displayNameCon1 = string.Empty;
            string thumbnailUrlCon1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardExchangeRequested), (string deviceId, string displayName, string thumbnailUrl) =>
            {
                deviceIdReqCon1 = deviceId;
                displayNameCon1 = displayName;
                thumbnailUrlCon1 = thumbnailUrl;

                connection1.SendAsync("AcceptCardExchange", deviceId, DeviceId1, "displayName1", "cardData1");
            });
            string waitingForAcceptanceFromDeviceCon1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.WaitingForAcceptance), (string peerDeviceId) =>
            {
                waitingForAcceptanceFromDeviceCon1 = peerDeviceId;
            });


            string cardExchangeAcceptedPeerDeviceId1 = string.Empty;
            string cardExchangeAcceptedPeerDisplayName1 = string.Empty;
            string cardExchangeAcceptedPeerCardData1 = string.Empty;
            string cardExchangeAcceptedPeerImageUrl1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardExchangeAccepted), (string peerDeviceId, string peerDisplayName, string peerCardData, string peerImageUrl) =>
            {
                cardExchangeAcceptedPeerDeviceId1 = peerDeviceId;
                cardExchangeAcceptedPeerDisplayName1 = peerDisplayName;
                cardExchangeAcceptedPeerCardData1 = peerCardData;
                cardExchangeAcceptedPeerImageUrl1 = peerImageUrl;

                connection1.SendAsync("SendCardData", DeviceId1, peerDeviceId, "displayName1", "cardData1");
            });
            string acceptanceSentDeviceCon1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.AcceptanceSent), (string deviceId) =>
            {
                acceptanceSentDeviceCon1 = deviceId;
            });

            string cardDataReceivedDeviceId1 = string.Empty;
            string cardDataReceivedDisplayName1 = string.Empty;
            string cardDataReceivedCardData1 = string.Empty;
            string cardDataReceivedImageUrl1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardDataReceived), (string deviceId, string displayName, string cardData, string imageUrl) =>
            {
                cardDataReceivedDeviceId1 = deviceId;
                cardDataReceivedDisplayName1 = displayName;
                cardDataReceivedCardData1 = cardData;
                cardDataReceivedImageUrl1 = imageUrl;
            });
            string cardDataSentDeviceCon1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardDataSent), (string peerDeviceId) =>
            {
                cardDataSentDeviceCon1 = peerDeviceId;
            });

            //Connection 2
            string deviceIdReqCon2 = string.Empty;
            string displayNameCon2 = string.Empty;
            string thumbnailUrlCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.CardExchangeRequested), (string deviceId, string displayName, string thumbnailUrl) =>
            {
                deviceIdReqCon2 = deviceId;
                displayNameCon2 = displayName;
                thumbnailUrlCon2 = thumbnailUrl;

                connection2.SendAsync("AcceptCardExchange", deviceId, DeviceId2, "displayName2", "cardData2");
            });
            string waitingForAcceptanceFromDeviceCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.WaitingForAcceptance), (string peerDeviceId) =>
            {
                waitingForAcceptanceFromDeviceCon2 = peerDeviceId;
            });

            string cardExchangeAcceptedPeerDeviceId2 = string.Empty;
            string cardExchangeAcceptedPeerDisplayName2 = string.Empty;
            string cardExchangeAcceptedPeerCardData2 = string.Empty;
            string cardExchangeAcceptedPeerImageUrl2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.CardExchangeAccepted), (string peerDeviceId, string peerDisplayName, string peerCardData, string peerImageUrl) =>
            {
                cardExchangeAcceptedPeerDeviceId2 = peerDeviceId;
                cardExchangeAcceptedPeerDisplayName2 = peerDisplayName;
                cardExchangeAcceptedPeerCardData2 = peerCardData;
                cardExchangeAcceptedPeerImageUrl2 = peerImageUrl;

                connection2.SendAsync("SendCardData", DeviceId2, peerDeviceId, "displayName2", "cardData2");
            });
            string acceptanceSentDeviceCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.AcceptanceSent), (string deviceId) =>
            {
                acceptanceSentDeviceCon2 = deviceId;
            });

            string cardDataReceivedDeviceId2 = string.Empty;
            string cardDataReceivedDisplayName2 = string.Empty;
            string cardDataReceivedCardData2 = string.Empty;
            string cardDataReceivedImageUrl2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.CardDataReceived), (string deviceId, string displayName, string cardData, string imageUrl) =>
            {
                cardDataReceivedDeviceId2 = deviceId;
                cardDataReceivedDisplayName2 = displayName;
                cardDataReceivedCardData2 = cardData;
                cardDataReceivedImageUrl2 = imageUrl;
            });
            string cardDataSentDeviceCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.CardDataSent), (string peerDeviceId) =>
            {
                cardDataSentDeviceCon2 = peerDeviceId;
            });

            // Actions
            await connection1.SendAsync("Update", DeviceId1, Longitude, Latitude1, "displayName1");
            await connection2.SendAsync("Update", DeviceId2, Longitude, LatitudeIn2, "displayName2");

            await connection1.SendAsync("RequestCardExchange", DeviceId1, DeviceId2, "displayName1");
            await connection2.SendAsync("RequestCardExchange", DeviceId2, DeviceId1, "displayName2");

            await Task.Delay(2000);

            //Asserts
            deviceIdReqCon1.Should().Be(DeviceId2);
            displayNameCon1.Should().Be("displayName2");
            thumbnailUrlCon1.Should().NotBeNullOrEmpty();
            deviceIdReqCon2.Should().Be(DeviceId1);
            displayNameCon2.Should().Be("displayName1");
            thumbnailUrlCon2.Should().NotBeNullOrEmpty();

            waitingForAcceptanceFromDeviceCon1.Should().Be(DeviceId2);
            waitingForAcceptanceFromDeviceCon2.Should().Be(DeviceId1);

            cardExchangeAcceptedPeerDeviceId1.Should().Be(DeviceId2);
            cardExchangeAcceptedPeerDisplayName1.Should().Be("displayName2");
            cardExchangeAcceptedPeerCardData1.Should().Be("cardData2");
            cardExchangeAcceptedPeerImageUrl1.Should().NotBeNullOrEmpty();
            cardExchangeAcceptedPeerImageUrl2.Should().NotBeNullOrEmpty();
            acceptanceSentDeviceCon1.Should().Be(DeviceId2);
            cardExchangeAcceptedPeerDeviceId2.Should().Be(DeviceId1);
            cardExchangeAcceptedPeerDisplayName2.Should().Be("displayName1");
            cardExchangeAcceptedPeerCardData2.Should().Be("cardData1");
            acceptanceSentDeviceCon2.Should().Be(DeviceId1);

            cardDataReceivedDeviceId1.Should().Be(DeviceId2);
            cardDataReceivedDisplayName1.Should().Be("displayName2");
            cardDataReceivedCardData1.Should().Be("cardData2");
            cardDataSentDeviceCon1.Should().Be(DeviceId2);
            cardDataReceivedDeviceId2.Should().Be(DeviceId1);
            cardDataReceivedDisplayName2.Should().Be("displayName1");
            cardDataReceivedCardData2.Should().Be("cardData1");
            cardDataReceivedImageUrl1.Should().NotBeNullOrEmpty();
            cardDataReceivedImageUrl2.Should().NotBeNullOrEmpty();
            cardDataSentDeviceCon2.Should().Be(DeviceId1);
        }

        [Fact]
        public async void ConnectionTest_Swap2Subscribers_RevokeCardExchangeRequest_SubscribedCalled()
        {
            //Connection
            var connection1 = CreateConnection();
            var connection2 = CreateConnection();

            await connection1.StartAsync().ContinueWith(x =>
            {
                if (connection1.State == HubConnectionState.Connected)
                {
                    connection1.SendAsync("Subscribe", DeviceId1, Longitude, Latitude1, "displayName1", _bse64StringImage1);
                    ;
                }
            });

            await connection2.StartAsync().ContinueWith(x =>
            {
                if (connection2.State == HubConnectionState.Connected)
                {
                    connection2.SendAsync("Subscribe", DeviceId2, Longitude, LatitudeIn2, "displayName2", _bse64StringImage2);

                }
            });


            // Events to handle
            //Connection 1
            string deviceIdReqCon1 = string.Empty;
            string displayNameCon1 = string.Empty;
            string thumbUrlCon1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardExchangeRequested), (string deviceId, string displayName, string thumbUrl) =>
            {
                deviceIdReqCon1 = deviceId;
                displayNameCon1 = displayName;
                thumbUrlCon1 = thumbUrl;

                connection1.SendAsync("AcceptCardExchange", deviceId, DeviceId1, "displayName1", "cardData1");
            });

            string cardExchangeAcceptedPeerDeviceId1 = string.Empty;
            string cardExchangeAcceptedPeerDisplayName1 = string.Empty;
            string cardExchangeAcceptedPeerCardData1 = string.Empty;
            string cardExchangeAcceptedPeerImageUrl1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardExchangeAccepted), (string peerDeviceId, string peerDisplayName, string peerCardData, string peerImageUrl) =>
            {
                cardExchangeAcceptedPeerDeviceId1 = peerDeviceId;
                cardExchangeAcceptedPeerDisplayName1 = peerDisplayName;
                cardExchangeAcceptedPeerCardData1 = peerCardData;
                cardExchangeAcceptedPeerImageUrl1 = peerImageUrl;
            });
            string cardExchangeRequestRevokedDeviceId1 = string.Empty;
            connection1.On(nameof(ICardExchangeClient.CardExchangeRequestRevoked), (string deviceId) =>
            {
                cardExchangeRequestRevokedDeviceId1 = deviceId;
            });

            //Connection 2
            string deviceIdReqCon2 = string.Empty;
            string displayNameCon2 = string.Empty;
            string thumbUrlCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.CardExchangeRequested), (string deviceId, string displayName, string thumbUrl) =>
            {
                deviceIdReqCon2 = deviceId;
                displayNameCon2 = displayName;
                thumbUrlCon2 = thumbUrl;

                connection2.SendAsync("RevokeCardExchangeRequest", DeviceId2, deviceId);
            });
            string revokeSentDeviceCon2 = string.Empty;
            connection2.On(nameof(ICardExchangeClient.RevokeSent), (string deviceId) =>
            {
                revokeSentDeviceCon2 = deviceId;
            });

            // Actions
            await connection1.SendAsync("Update", DeviceId1, Longitude, Latitude1, "displayName1");
            await connection2.SendAsync("Update", DeviceId2, Longitude, LatitudeIn2, "displayName2");

            await connection1.SendAsync("RequestCardExchange", DeviceId1, DeviceId2, "displayName1");
            await connection2.SendAsync("RequestCardExchange", DeviceId2, DeviceId1, "displayName2");

            await Task.Delay(8000);

            //Asserts
            deviceIdReqCon1.Should().Be(DeviceId2);
            displayNameCon1.Should().Be("displayName2");
            thumbUrlCon1.Should().NotBeNullOrEmpty();
            deviceIdReqCon2.Should().Be(DeviceId1);
            displayNameCon2.Should().Be("displayName1");
            thumbUrlCon2.Should().NotBeNullOrEmpty();

            //should not be called
            cardExchangeAcceptedPeerDeviceId1.Should().Be(String.Empty);
            cardExchangeAcceptedPeerDisplayName1.Should().Be(String.Empty);
            cardExchangeAcceptedPeerCardData1.Should().Be(String.Empty);

            cardExchangeRequestRevokedDeviceId1.Should().Be(DeviceId2);
            revokeSentDeviceCon2.Should().Be(DeviceId1);
        }
    }
}
