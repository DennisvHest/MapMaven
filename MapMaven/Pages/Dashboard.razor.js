const dashboard = (function () {
    function rankHistoryTooltip(chartData) {
        const dataItem = chartData.w.config.series[chartData.seriesIndex].data[chartData.dataPointIndex];

        const container = createContainer();
        const header = createHeader(dataItem);
        const rank = createRank(dataItem);
        const mapList = createMapList(dataItem.extra);

        container.appendChild(header);
        container.appendChild(rank);
        container.appendChild(mapList);

        return container.outerHTML;
    }

    function createContainer() {
        const container = document.createElement('div');
        container.className = 'arrow_box';
        container.style.padding = '5px';

        return container;
    }

    function createHeader(dataItem) {
        const header = document.createElement('div');
        header.style.margin = '-5px -5px 0 -5px';
        header.style.padding = '5px';
        header.style.backgroundColor = 'black';

        const value = document.createElement('b');
        value.textContent = dataItem.x;
        header.appendChild(value);

        return header;
    }

    function createRank(dataItem) {
        const rank = document.createElement('div');
        rank.style.padding = '5px 0';

        const rankLabel = document.createElement('b');
        rankLabel.textContent = 'Rank: ';
        rank.appendChild(rankLabel);

        const rankValue = document.createTextNode(dataItem.y);
        rank.appendChild(rankValue);

        return rank;
    }

    function createMapList(data) {
        const mapList = document.createElement('ul');
        mapList.style.paddingTop = '5px';
        mapList.style.borderTop = '1px solid grey';

        for (let map of data) {
            const li = document.createElement('li');
            li.style.display = 'flex';
            li.style.gap = '5px';
            li.style.alignItems = 'center';

            const coverImage = document.createElement('img');
            coverImage.src = map.coverImageUrl;
            coverImage.style.width = '16px';
            coverImage.style.height = '16px';
            li.appendChild(coverImage);

            const mapName = document.createElement('span');
            mapName.textContent = map.name;

            li.appendChild(mapName);

            const difficultyTag = document.createElement('div');
            difficultyTag.className = 'mud-chip mud-chip-outlined mud-schip-size-extra-small mud-chip-color-default';
            difficultyTag.style.border = `1px solid ${map.difficultyColor}`;
            difficultyTag.style.margin = '5px 0';

            const difficulty = document.createElement('span');
            difficulty.className = 'mud-chip-content';
            difficulty.textContent = map.difficulty;

            difficultyTag.appendChild(difficulty);

            const ppTag = document.createElement('div');
            ppTag.className = 'mud-chip mud-chip-filled mud-schip-size-extra-small mud-chip-color-default';
            ppTag.style.margin = '5px 0';

            const pp = document.createElement('span');
            pp.className = 'mud-chip-content';
            pp.innerHTML = `${map.pp} <b class='ml-1'>pp<b/>`;

            ppTag.appendChild(pp);

            li.appendChild(difficultyTag);
            li.appendChild(ppTag);

            mapList.appendChild(li);
        }

        return mapList;
    }

    return {
        rankHistoryTooltip: rankHistoryTooltip
    };
})();