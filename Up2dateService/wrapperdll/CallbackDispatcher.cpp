#include <iostream>
#include <sstream>
#include <fstream>
#include "ddi.hpp"
#include "CallbackDispatcher.hpp"

using namespace ddi;

namespace HkbClient {

    void CallbackDispatcher::SetConfig(const std::vector<KEYVALUEPAIR> config) {
        configInfo = config;
    }

    void CallbackDispatcher::SetDownloadLocation(std::string location) {
        downloadLocation = location;
    }

    std::unique_ptr<ConfigResponse> CallbackDispatcher::onConfigRequest() {
        auto builder = ConfigResponseBuilder::newInstance();
        configRequest(builder.get());

        return builder->build();
    }

    std::unique_ptr<Response> CallbackDispatcher::onDeploymentAction(std::unique_ptr<DeploymentBase> dp) {
        auto builder = ResponseBuilder::newInstance();

        DEPLOYMENTINFO info;
        info.id = dp->getId();
        info.updateType = dp->getUpdateType();
        info.downloadType = dp->getDownloadType();
        info.isInMaintenanceWindow = dp->isInMaintenanceWindow();

        Response::Execution execution = Response::Execution::CLOSED;
        Response::Finished finished = Response::Finished::SUCCESS;
        std::string message;
        for (const auto& chunk : dp->getChunks()) {
            try
            {
                info.chunkName = chunk->getName();
                info.chunkPart = chunk->getPart();
                info.chunkVersion = chunk->getVersion();
                for (const auto& artifact : chunk->getArtifacts()) {
                    info.artifactFileName = artifact->getFilename();
                    info.artifactFileHashMd5 = artifact->getFileHashes().md5;
                    info.artifactFileHashSha1 = artifact->getFileHashes().sha1;
                    info.artifactFileHashSha256 = artifact->getFileHashes().sha256;
                    ClientResult result;
                    DeployArtifact(artifact, info, result);
                    execution = result.execution;
                    finished = result.finished;
                    message = std::string(result.message);

                    break; // so far only single artifact in the chunk is supported
                }
            }
            catch(std::exception& e)
            {
                execution = Response::Execution::CLOSED;
                finished = Response::Finished::FAILURE;
                message = "Internal exception occured: " + std::string(e.what());
            }

            break; // so far only single chunk is supported
        }

        std::istringstream strm(message);
        std::string s;
        while (std::getline(strm, s)) {
            builder->addDetail(s);
        }

        if (execution == Response::Execution::CLOSED) {
            builder->setIgnoreSleep();
        }

        return builder
            ->setExecution(execution)
            ->setFinished(finished)
            ->setResponseDeliveryListener(std::shared_ptr<ResponseDeliveryListener>(new DeploymentBaseFeedbackDeliveryListener()))
            ->build();
    }

    std::unique_ptr<Response> CallbackDispatcher::onCancelAction(std::unique_ptr<CancelAction> action) {
        bool cancelled = cancelAction(action->getStopId());
        
        return ResponseBuilder::newInstance()
                ->setExecution(Response::Execution::CLOSED)
                ->setFinished(cancelled ? Response::Finished::SUCCESS : Response::Finished::FAILURE)
                ->setResponseDeliveryListener(
                        std::shared_ptr<ResponseDeliveryListener>(new CancelActionFeedbackDeliveryListener()))
                ->setIgnoreSleep()
                ->build();
    }

    void CallbackDispatcher::onNoActions() {
    }

    void CallbackDispatcher::DeployArtifact(const std::shared_ptr<::Artifact> artifact, DEPLOYMENTINFO info, ClientResult& result )
    {
        _DEPLOYMENTINFO callback_info;
        callback_info.id = info.id;
        callback_info.updateType = info.updateType.c_str();
        callback_info.downloadType = info.downloadType.c_str();
        callback_info.isInMaintenanceWindow = info.isInMaintenanceWindow;
        callback_info.chunkPart = info.chunkPart.c_str();
        callback_info.chunkName = info.chunkName.c_str();
        callback_info.chunkVersion = info.chunkVersion.c_str();
        callback_info.artifactFileName = info.artifactFileName.c_str();
        callback_info.artifactFileHashMd5 = info.artifactFileHashMd5.c_str();
        callback_info.artifactFileHashSha1 = info.artifactFileHashSha1.c_str();
        callback_info.artifactFileHashSha256 = info.artifactFileHashSha256.c_str();

        deploymentAction(artifact.get(), callback_info, result);
    }

}
