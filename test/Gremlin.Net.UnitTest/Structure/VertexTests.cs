﻿using Gremlin.Net.Structure;
using Xunit;

namespace Gremlin.Net.UnitTest.Structure
{
    public class VertexTests
    {
        [Fact]
        public void VertexLabel_NoLabelSpecified_ShouldHaveDefaultLabel()
        {
            var vertex = new Vertex(1);

            Assert.Equal("vertex", vertex.Label);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5823)]
        public void VertexToString_WithValidId_IncludeId(object vertexId)
        {
            var vertex = new Vertex(vertexId);

            var vertexString = vertex.ToString();

            Assert.Equal($"v[{vertexId}]", vertexString);
        }

        [Fact]
        public void VertexLabel_LabelSpecified_ShouldReturnSpecifiedLabel()
        {
            const string specifiedLabel = "person";

            var vertex = new Vertex(1, specifiedLabel);
            
            Assert.Equal(specifiedLabel, vertex.Label);
        }

        [Fact]
        public void VertexEquals_EqualId_ReturnsTrue()
        {
            var firstVertex = new Vertex(1);
            var secondVertex = new Vertex(1);

            Assert.Equal(firstVertex, secondVertex);
        }
    }
}